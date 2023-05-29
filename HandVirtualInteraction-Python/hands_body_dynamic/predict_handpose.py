from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense
import cv2
import numpy as np
import mediapipe as mp

import time
'''
动态手势识别（LeftFlick，RightFlick，UpFlick，DownFlick，SnapFinger）
基础手势（index_finger,open_hand,None）
帧数：30帧
识别延迟一秒左右
模型：action600_30_4.h5
'''
# 视频流序列
sequence = []
#
sentence = []
threshold = 0.99
# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])
actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])

# label = "index"
# actions = np.array(['LeftFlick', 'RightFlick', 'UpFlick', 'DownFlick', 'SnapFinger'])

# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'SnapFinger'])
'''
action500_15_5.h5
action800_15_5.h5
'''

model_file = 'action1000_15_7.h5'
sequence_length = 15

colors = [(245, 117, 16), (117, 245, 16), (16, 117, 245), (16, 117, 245), (0, 191, 255), (199, 21, 133), (138, 43, 226)]#动作框，增加动作数目，加RGB元组


#定义并引用mediapipe中的hands模块（识别hands）
mpHands = mp.solutions.hands
hands = mpHands.Hands(max_num_hands=2,
                      min_detection_confidence=0.6,
                      min_tracking_confidence=0.6)
# 定义并引用mediapipe中的pose模块（识别body）
mp_holistic = mp.solutions.pose
bodys = mp_holistic.Pose(
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


point_max_area = [0 for i in range(6)]
# 判断手是否处于运动中
def hand_motion_detection(sequence, sequence_length):
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

    p1 = cal_area(body_leftwrist)
    p2 = cal_area(body_rightwrist)
    p3 = cal_area(lh_thumb)
    p4 = cal_area(lh_index)
    p5 = cal_area(rh_thumb)
    p6 = cal_area(rh_index)
    area_op = [p1, p2, p3, p4, p5, p6]
    # print(area_op, point_max_area[-2])
    # 面积最大值判断、赋值
    for one_area_index in range(len(point_max_area)):
        if area_op[one_area_index] > point_max_area[one_area_index]:
            point_max_area[one_area_index] = area_op[one_area_index]
    # 运动判断（>20%）
    motion_label = [0 for i in range(len(point_max_area))]
    for one_area_index in range(len(point_max_area)):
        if area_op[one_area_index] > point_max_area[one_area_index] * 0.08:
            motion_label[one_area_index] = 1
    if 1 in motion_label:
        # print("moving")
        return True
    else:
        # print("Stoping")
        return False


    # print(p1, six_point_area)
    # print(six_point_area)


def mediapipe_detection(image, model_hand, model_body):
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB) # COLOR CONVERSION BGR 2 RGB
    image.flags.writeable = False                  # Image is no longer writeable
    results_hand = model_hand.process(image)                 # Make prediction
    results_body = model_body.process(image)

    image.flags.writeable = True                   # Image is now writeable
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR) # COLOR COVERSION RGB 2 BGR
    return image, results_hand, results_body


def draw_styled_landmarks(image, results_hand, results_body):
    # #2d图像绘制，相机，屏幕坐标
    mpDraw.draw_landmarks(
        image, results_body.pose_landmarks, mp_holistic.POSE_CONNECTIONS)

    if results_hand.multi_hand_landmarks:
        for handLms in results_hand.multi_hand_landmarks:
            for id, lm in enumerate(handLms.landmark):
                h, w, c = image.shape
                cx, cy = int(lm.x * w), int(lm.y * h)
                cv2.circle(image, (cx, cy), 5, (255, 0, 255), cv2.FILLED)
            # 绘制手部特征点：
            mpDraw.draw_landmarks(image, handLms, mpHands.HAND_CONNECTIONS)


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
    return np.concatenate([pose, lh, rh])


def load_model(model_file):
    model = Sequential()
    model.add(LSTM(64, return_sequences=True, activation='relu', input_shape=(sequence_length, 258)))
    model.add(LSTM(128, return_sequences=True, activation='relu'))
    model.add(LSTM(64, return_sequences=False, activation='relu'))
    model.add(Dense(64, activation='relu'))
    model.add(Dense(32, activation='relu'))
    model.add(Dense(actions.shape[0], activation='softmax'))
    model.load_weights(model_file)
    return model


def prob_viz(res, actions, input_frame, colors):
    output_frame = input_frame.copy()
    for num, prob in enumerate(res):
        cv2.rectangle(output_frame, (0, 60 + num * 40), (int(prob * 100), 90 + num * 40), colors[num], -1)
        cv2.putText(output_frame, actions[num], (0, 85 + num * 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2,
                    cv2.LINE_AA)
    return output_frame


def predict_storage(result_action, predict_action):
    if result_action == "" or result_action != predict_action:
        return True
    else:
        return False


#存储识别结果
result_action = ""

model = load_model(model_file)
cap = cv2.VideoCapture(0)
# Set mediapipe mode
while cap.isOpened():
    # Read feed
    ret, frame = cap.read()
    # Make detections
    image, result_hand, result_body = mediapipe_detection(frame, hands, bodys)
    # print(results)
    # Draw landmarks
    draw_styled_landmarks(image, results_hand=result_hand, results_body=result_body)

    if result_hand.multi_hand_world_landmarks:  # 有手部数据
        # 2. Prediction logic
        keypoints = extract_keypoints(results_hand=result_hand, results_body=result_body)
        sequence.append(keypoints)
        sequence = sequence[-sequence_length:]
        if len(result_hand.multi_handedness) == 1:  # 如果检测到一只手，限制单手手势识别
            if len(sequence) == sequence_length:
                if hand_motion_detection(sequence, sequence_length):  # 运动判断
                    res = model.predict(np.expand_dims(sequence, axis=0))[0]
                # print(actions[np.argmax(res)])

                # 3. Viz logic
                if res[np.argmax(res)] > threshold:

                    predict_res = actions[np.argmax(res)]  # 一次识别存储输出

                    print(predict_res)
                    # sequence = []

                    # if predict_storage(result_action=result_action, predict_action=predict_res):
                    #     result_action = predict_res
                    #     print(result_action)
                    #     print(res[np.argmax(res)])

                if len(sentence) > 0:
                    if actions[np.argmax(res)] != sentence[-1]:
                        sentence.append(actions[np.argmax(res)])
                else:
                    sentence.append(actions[np.argmax(res)])
                if len(sentence) > 5:
                    sentence = sentence[-5:]
                # Viz probabilities
                image = prob_viz(res, actions, image, colors)
    cv2.rectangle(image, (0, 0), (640, 40), (245, 117, 16), -1)
    cv2.putText(image, ' '.join(sentence), (3, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2, cv2.LINE_AA)

    '''视频FPS计算'''
    cTime = time.time()
    fps = 1 / (cTime - pTime)
    pTime = cTime
    cv2.putText(image, str(int(fps)), (10, 70), cv2.FONT_HERSHEY_PLAIN, 3,
                (255, 0, 255), 3)  # FPS的字号，颜色等设置

    # Show to screen
    cv2.imshow('OpenCV Feed', image)
    # Break gracefully
    if cv2.waitKey(10) & 0xFF == ord('q'):
        break
cap.release()
cv2.destroyAllWindows()
