import cv2
import numpy as np
import os
import mediapipe as mp

mp_holistic = mp.solutions.holistic# Holistic model
mp_drawing = mp.solutions.drawing_utils # Drawing utilities

DATA_PATH = os.path.join('MP_Data')

actions = np.array(['IndexFinger_Right', 'IndexFinger_Forward', 'OpenHand_Right', 'OpenHand_Forward'])

# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])
# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'SnapFinger'])
# actions = np.array(['LeftFlick', 'RightFlick', 'UpFlick', 'DownFlick', 'SnapFinger'])

# 每组数据长度
no_sequences = 60
# 视频流帧长度
sequence_length = 10



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
    # pose = np.array([[res.x, res.y, res.z, res.visibility] for res in results_body.pose_landmarks.landmark]).flatten() if results_body.pose_landmarks else np.zeros(33*4)
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
    # return np.concatenate([pose, lh, rh])
    return np.concatenate([lh, rh])


for action in actions:
    for sequence in range(no_sequences):
        try:
            os.makedirs(os.path.join(DATA_PATH, action, str(sequence)))
        except:
            pass

cap = cv2.VideoCapture(0)


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


# NEW LOOP
# Loop through actions
for action in actions:
    # Loop through sequences aka videos
    for sequence in range(no_sequences):
        # Loop through video length aka sequence length
        for frame_num in range(sequence_length):
            # Read feed
            ret, frame = cap.read()
            # Make detections
            image, result_hand, result_body = mediapipe_detection(frame, hands, bodys)
            #                 print(results)
            # Draw landmarks
            draw_styled_landmarks(image, results_hand=result_hand, results_body=result_body)
            # NEW Apply wait logic
            if frame_num == 0:
                cv2.putText(image, 'STARTING COLLECTION', (120, 200),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 4, cv2.LINE_AA)
                cv2.putText(image, ' {} {}'.format(action, sequence), (15, 20),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 1, cv2.LINE_AA)
                # Show to screen
                cv2.imshow('OpenCV Feed', image)
                cv2.waitKey(2000)
            else:
                cv2.putText(image, ' {} {}'.format(action, sequence), (15, 20),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 1, cv2.LINE_AA)
                # Show to screen
                cv2.imshow('OpenCV Feed', image)
            # NEW Export keypoints
            keypoints = extract_keypoints(results_hand=result_hand, results_body=result_body)
            npy_path = os.path.join(DATA_PATH, action, str(sequence), str(frame_num))
            np.save(npy_path, keypoints)
            # Break gracefully
            if cv2.waitKey(10) & 0xFF == ord('q'):
                break
cap.release()
cv2.destroyAllWindows()
