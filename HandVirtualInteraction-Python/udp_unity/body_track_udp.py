import cv2
import mediapipe as mp
import time
import socket
import numpy as np
import math
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense

#udp传输
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)
#摄像头
cap = cv2.VideoCapture(0)       #OpenCV摄像头调用：0=内置摄像头（笔记本）   1=USB摄像头-1  2=USB摄像头-2
#窗口大小
# cap.set(3, 1280)
# cap.set(4, 720)
# 基础手势
base_actions_label = ["IndexFinger", "OpenHand"]
# 触发类手势参数
trigger_model_file = "./dynamic_model/action1000_15_7.h5"
# trigger_actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])
trigger_actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])
trigger_threshold = 0.99
trigger_sequence_lenth = 15
trigger_data_lenth = 258

# 静态手势参数
static_model_file = "./static_model/action800_10_4.h5"
static_actions = np.array(['IndexFinger_Right', 'IndexFinger_Forward', 'OpenHand_Right', 'OpenHand_Forward'])
static_sequence_lenth = 10
static_threshold = 0.98
static_data_lenth = 126

# 动态手势、手部姿态标签
action_key = "ActionKey"
static_action_key = "StaticActionKey"

actions_labellist = ["LeftFlick", "RightFlick", "UpFlick", "DownFlick", "SnapFinger"]
# mediapipe 身体关键点参数、udp传输标签
handlabel_list = ["RightHand", "LeftHand"]
bodylabel_list = ["RightWrist", "LeftWrist",
                  "RightElbow", "LeftElbow",
                  "RightArm", "LeftArm",
                  "RightHip", "LeftHip"]
bodylabel_indexlist = [16, 15, 14, 13, 12, 11, 24, 23]
distance_label = ["RightHandDistance", "LeftHandDistance"]


#定义并引用mediapipe中的hands模块（识别hands）
mpHands = mp.solutions.hands
hands = mpHands.Hands(max_num_hands=2,
                      min_detection_confidence=0.6,
                      min_tracking_confidence=0.6)
# 定义并引用mediapipe中的pose模块（识别body）
mp_holistic = mp.solutions.pose
holistic = mp_holistic.Pose(
        model_complexity=0,
        smooth_landmarks=True,# 是否选择平滑关键点
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5)
mp_drawing_styles = mp.solutions.drawing_styles
# 定义并引用mediapipe中的绘制模块
mpDraw = mp.solutions.drawing_utils

# 帧率时间计算
pTime = 0
cTime = 0
# 找到手掌间的距离和实际的手与摄像机之间的距离的映射关系，x 代表手掌间的距离(像素距离)，y 代表手和摄像机之间的距离(cm)
x = [300, 245, 200, 170, 145, 130, 112, 103, 93, 87, 80, 75, 70, 67, 62, 59, 57]
y = [20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100]
coff = np.polyfit(x, y, 2)  # 构造二阶多项式方程

def draw_styled_landmarks(img, results_hand, results_body):
    # #2d图像绘制，相机，屏幕坐标
    mpDraw.draw_landmarks(
        img, results_body.pose_landmarks, mp_holistic.POSE_CONNECTIONS)

    if results_hand.multi_hand_landmarks:
        for handLms in results_hand.multi_hand_landmarks:
            for id, lm in enumerate(handLms.landmark):
                h, w, c = img.shape
                cx, cy = int(lm.x * w), int(lm.y * h)
                cv2.circle(img, (cx, cy), 5, (255, 0, 255), cv2.FILLED)
            # 绘制手部特征点：
            mpDraw.draw_landmarks(img, handLms, mpHands.HAND_CONNECTIONS)


def extract_keypoints(results_hand, results_body):
    pose = np.array([[res.x, res.y, res.z, res.visibility] for res in results_body.pose_landmarks.landmark]).flatten() if results_body.pose_landmarks else np.zeros(33*4)
    lh = np.zeros(21 * 3)
    rh = np.zeros(21 * 3)
    if results_hand.multi_hand_world_landmarks:
        if len(results_hand.multi_handedness) == 1:  # 如果检测到一只手
            label = results_hand.multi_handedness[0].classification[0].label  # 获得Label判断是哪几手
            # label修正（摄像头镜像识别修正）
            if "Left" == label:
                rh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[0].landmark]).flatten()
                lh = np.zeros(21 * 3)
            elif "Right" == label:
                lh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[0].landmark]).flatten()
                rh = np.zeros(21 * 3)
        else:
            label = results_hand.multi_handedness[0].classification[0].label  # 获得Label判断是哪几手
            # label修正（摄像头镜像识别修正）
            if "Left" == label:
                rh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[0].landmark]).flatten()
                lh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[1].landmark]).flatten()
            elif "Right" == label:
                rh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[1].landmark]).flatten()
                lh = np.array([[res.x, res.y, res.z] for res in results_hand.multi_hand_landmarks[0].landmark]).flatten()
    return np.concatenate([pose, lh, rh]), np.concatenate([lh, rh])


def load_model(model_file, sequence_length, actions, data_lenth):
    model = Sequential()
    model.add(LSTM(64, return_sequences=True, activation='relu', input_shape=(sequence_length, data_lenth)))
    model.add(LSTM(128, return_sequences=True, activation='relu'))
    model.add(LSTM(64, return_sequences=False, activation='relu'))
    model.add(Dense(64, activation='relu'))
    model.add(Dense(32, activation='relu'))
    model.add(Dense(actions.shape[0], activation='softmax'))
    model.load_weights(model_file)
    return model


# 计算欧氏距离
def calEuclidean(x, y):
    dist = np.sqrt(np.sum(np.square(x-y)))   # 注意：np.array 类型的数据可以直接进行向量、矩阵加减运算。np.square 是对每个元素求平均~~~~
    return dist


# 通过给到的点运动数据计算起始、中间、结束三点的三角形面积
def cal_area(point_list):
    point_1 = point_list[0]
    point_2 = point_list[len(point_list)//2]
    point_3 = point_list[-1]
    a = calEuclidean(np.array(point_1), np.array(point_2))
    b = calEuclidean(np.array(point_1), np.array(point_3))
    c = calEuclidean(np.array(point_2), np.array(point_3))
    # 计算半周长
    s = (a + b + c) / 2
    # 计算面积
    area = (s * (s - a) * (s - b) * (s - c)) ** 0.5
    return area


# 判断手是否处于运动中
def hand_motion_detection(results_hand, sequence, sequence_length):
    body_leftwrist = []
    body_rightwrist = []
    lh_thumb = []
    lh_index = []
    rh_thumb = []
    rh_index = []
    for i in range(sequence_length):
        body = sequence[i][:33*4]
        lh = sequence[i][33*4:33*4+21*3]
        rh = sequence[i][33*4+21*3:]
        body_leftwrist.append([body[15 * 4], body[15 * 4 + 1], body[15 * 4 + 2]])
        body_rightwrist.append([body[16 * 4], body[16 * 4 + 1], body[16 * 4 + 2]])
        lh_thumb.append([lh[4 * 3], lh[4 * 3 + 1], lh[4 * 3 + 2]])
        lh_index.append([lh[8 * 3], lh[8 * 3 + 1], lh[8 * 3 + 2]])
        rh_thumb.append([rh[4 * 3], rh[4 * 3 + 1], rh[4 * 3 + 2]])
        rh_index.append([rh[8 * 3], rh[8 * 3 + 1], rh[8 * 3 + 2]])

    p2 = cal_area(body_rightwrist)
    p5 = cal_area(rh_thumb)
    p6 = cal_area(rh_index)
    p1 = cal_area(body_leftwrist)
    p3 = cal_area(lh_thumb)
    p4 = cal_area(lh_index)

    area_op = [[p2, p5, p6], [p1, p3, p4]]
    # print(area_op)
    # # 面积最大值判断、赋值
    # for one_area_index in range(len(point_max_area)):
    #     if area_op[one_area_index] > point_max_area[one_area_index]:
    #         point_max_area[one_area_index] = area_op[one_area_index]
    #
    # # 运动判断（>20%）
    # motion_label = [0 for i in range(len(point_max_area))]
    # for one_area_index in range(len(point_max_area)):
    #     if area_op[one_area_index] > point_max_area[one_area_index] * 0.03:
    #         motion_label[one_area_index] = 1
    # 判断当前左右手，并进行运动判断
    if results_hand.multi_hand_world_landmarks:  # 有手部数据
        # 左右手判断
        if len(results_hand.multi_handedness) == 1:  # 如果检测到一只手
            label = results_hand.multi_handedness[0].classification[0].label  # 获得Label判断是哪几手
            #左右手修正
            if "Left" == label:
                label = handlabel_list[0]
            elif "Right" == label:
                label = handlabel_list[1]


            # 运动判断
            for one_hand_area in area_op[handlabel_list.index(label)]:
                if one_hand_area > 0.002:
                    return True
            else:
                return False
    else:
        return False





sequence = []
sequence_hand = []
static_model = load_model(static_model_file, sequence_length=static_sequence_lenth, actions=static_actions, data_lenth=static_data_lenth)
trigger_model = load_model(trigger_model_file, sequence_length=trigger_sequence_lenth, actions=trigger_actions, data_lenth=trigger_data_lenth)
trigger_result_action = ""
while True:
    # udp传输数据表
    data_3d = {}

    success, img = cap.read()
    imgRGB = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)       #cv2图像初始化
    results_hand = hands.process(imgRGB)
    results_body = holistic.process(imgRGB)

    # 动态、静态手势手势识别
    if results_hand.multi_hand_world_landmarks:  # 有手部数据
        keypoints, keypoints_hand = extract_keypoints(results_hand=results_hand, results_body=results_body)
        sequence_hand.append(keypoints_hand)
        sequence_hand = sequence_hand[- 2 * static_sequence_lenth:]
        sequence.append(keypoints)
        sequence = sequence[- 2 * trigger_sequence_lenth:]

        # predict
        static_sequence = sequence_hand[-static_sequence_lenth:]
        trigger_sequence = sequence[-trigger_sequence_lenth:]
        if len(results_hand.multi_handedness) == 1:  # 如果检测到一只手，限制单手手势识别
            static_result_action = ""
            trigger_result_action = ""

            # static
            if len(static_sequence) == static_sequence_lenth:  # 视频流长度限制
                static_res = static_model.predict(np.expand_dims(static_sequence, axis=0))[0]
                if static_res[np.argmax(static_res)] > static_threshold:
                    static_result_action = static_actions[np.argmax(static_res)]
                    data_3d[static_action_key] = static_result_action
                    # print("static:", static_result_action)

            # trigger
            if len(trigger_sequence) == trigger_sequence_lenth:  # 视频流长度限制
                if hand_motion_detection(results_hand, sequence, trigger_sequence_lenth):# 运动判断

                    trigger_res = trigger_model.predict(np.expand_dims(trigger_sequence, axis=0))[0]
                    if trigger_res[np.argmax(trigger_res)] > trigger_threshold:  # 阈值判断
                        trigger_result_action = trigger_actions[np.argmax(trigger_res)]  # 识别结果

            predict_result = ""
            if static_result_action != "" and trigger_result_action != "":
                # 静态结果与动态结果是否相同基础手势判断
                op_flag = True
                for base_action in base_actions_label:
                    if base_action in static_result_action and base_action in trigger_result_action:
                        # print("result action:", trigger_result_action)
                        predict_result = trigger_result_action
                        op_flag = False
                if op_flag:
                    # print("trigger action:", trigger_result_action)
                    predict_result = trigger_result_action
                sequence = []
                print("predict result:", predict_result)

                # 当前手势判断
                if predict_result:
                    for one_aciton_label in actions_labellist:
                        if one_aciton_label in predict_result:
                            data_3d[action_key] = one_aciton_label



    # print(predict_data)

    del_str = ["landmark", "{", "}", "x:", "y:", "z:"]
    # 身体关键点识别
    if results_body.pose_world_landmarks:  # 有身体数据
        # label数据逐个获取
        for one_labelindex in bodylabel_indexlist:
            if results_body.pose_world_landmarks.landmark[one_labelindex]:
                one_hand_landmark = []
                lefthand_pose_landmark = results_body.pose_world_landmarks.landmark[one_labelindex]
                lefthand_landmark_str = str(lefthand_pose_landmark)
                for i in lefthand_landmark_str.split():
                    if i == "visibility:":
                        break
                    if i not in del_str:
                        one_hand_landmark.append(float(i))
                data_3d[bodylabel_list[bodylabel_indexlist.index(one_labelindex)]] = one_hand_landmark

    # 手部关键点识别
    if results_hand.multi_hand_world_landmarks:  # 有手部数据
        # 左右手判断
        if len(results_hand.multi_handedness) == 1:  # 如果检测到一只手
            label = results_hand.multi_handedness[0].classification[0].label  # 获得Label判断是哪几手
            # label修正（摄像头镜像识别修正）
            if "Left" == label:
                label = handlabel_list[0]
                distancelabel = distance_label[0]
            elif "Right" == label:
                label = handlabel_list[1]
                distancelabel = distance_label[1]
            hand_landmarks = results_hand.multi_hand_world_landmarks[0]
            # 坐标数据处理
            one_hand_landmark = []
            landmark_str = str(hand_landmarks)
            for j in landmark_str.split():
                if j not in del_str:
                    one_hand_landmark.append(float(j))
            data_3d[label] = one_hand_landmark

            # 手部屏幕坐标和获取
            landmark_screen = []
            op_multi_hand_landmark = []
            for i in results_hand.multi_hand_landmarks:
                op_multi_hand_landmark.append(i)
            one_landmark = []
            handLms = results_hand.multi_hand_landmarks[0]
            for id, lm in enumerate(handLms.landmark):
                h, w, c = img.shape
                cx, cy = int(lm.x * w), int(lm.y * h)
                one_landmark = [cx, cy]
                landmark_screen.append(one_landmark)
            # 计算距离distance
            Lx1, Ly1 = landmark_screen[5]
            Lx2, Ly2 = landmark_screen[17]
            Ldistance = int(math.sqrt((Ly2 - Ly1) ** 2 + (Lx2 - Lx1) ** 2))
            A, B, C = coff
            LdistanceCM = A * Ldistance ** 2 + B * Ldistance + C
            LdistanceM = (1 - (LdistanceCM * 0.01))
            data_3d[distancelabel] = LdistanceM
            # print(label, LdistanceM)

        else:  # 如果检测到两只手
            # 调整左右手识别先后顺序（先进先识别改为始终右手先识别）
            if results_hand.multi_handedness[0].classification[0].label == "Right":
                op1 = results_hand.multi_handedness[0]
                results_hand.multi_handedness[0] = results_hand.multi_handedness[1]
                results_hand.multi_handedness[1] = op1
                op2 = results_hand.multi_hand_world_landmarks[0]
                results_hand.multi_hand_world_landmarks[0] = results_hand.multi_hand_world_landmarks[1]
                results_hand.multi_hand_world_landmarks[1] = op2
            # 手部关键点数据获取
            for i in range(len(results_hand.multi_handedness)):
                label = results_hand.multi_handedness[i].classification[0].label  # 获得Label判断是哪几手
                # label修正（摄像头镜像识别修正）
                if "Left" == label:
                    label = handlabel_list[0]
                    distancelabel = distance_label[0]
                elif "Right" == label:
                    label = handlabel_list[1]
                    distancelabel = distance_label[1]
                index = results_hand.multi_handedness[i].classification[0].index  # 获取左右手的索引号
                hand_landmarks = results_hand.multi_hand_world_landmarks[index]
                # 坐标数据处理
                one_hand_landmark = []
                landmark_str = str(hand_landmarks)
                del_str = ["landmark", "{", "}", "x:", "y:", "z:"]
                for j in landmark_str.split():
                    if j not in del_str:
                        one_hand_landmark.append(float(j))
                data_3d[label] = one_hand_landmark

                # 手部屏幕坐标和获取
                landmark_screen = []
                op_multi_hand_landmark = []
                for op in results_hand.multi_hand_landmarks:
                    op_multi_hand_landmark.append(op)
                one_landmark = []
                handLms = results_hand.multi_hand_landmarks[index]
                for id, lm in enumerate(handLms.landmark):
                    h, w, c = img.shape
                    cx, cy = int(lm.x * w), int(lm.y * h)
                    one_landmark = [cx, cy]
                    landmark_screen.append(one_landmark)
                # 计算距离distance
                Lx1, Ly1 = landmark_screen[5]
                Lx2, Ly2 = landmark_screen[17]
                Ldistance = int(math.sqrt((Ly2 - Ly1) ** 2 + (Lx2 - Lx1) ** 2))
                A, B, C = coff
                LdistanceCM = A * Ldistance ** 2 + B * Ldistance + C
                LdistanceM = (1 - (LdistanceCM * 0.01))
                data_3d[distancelabel] = LdistanceM
                # print(label, LdistanceM)

    # print("data3d:", data_3d)
    # #数据传输
    sock.sendto(str.encode(str(data_3d)), serverAddressPort)
    #绘图输出
    draw_styled_landmarks(img, results_hand, results_body)


    # # 身体三维模型
    # mpDraw.plot_landmarks(results_body.pose_world_landmarks, mp_holistic.POSE_CONNECTIONS)
    # ?
    # mpDraw.plot_landmarks(results_hand.multi_hand_world_landmarks, mpHands.POSE_CONNECTIONS)

    '''视频FPS计算'''
    cTime = time.time()
    fps = 1 / (cTime - pTime)
    pTime = cTime
    cv2.putText(img, str(int(fps)), (10, 70), cv2.FONT_HERSHEY_PLAIN, 3,
                (255, 0, 255), 3)       #FPS的字号，颜色等设置

    cv2.imshow("HandsImage", img)
    cv2.waitKey(1)

