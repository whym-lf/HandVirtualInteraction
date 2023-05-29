import cv2
import cvzone
from cvzone.HandTrackingModule import HandDetector
import time
import math

# （1）捕获摄像头
cap = cv2.VideoCapture(0)  # 捕获电脑摄像头
cap.set(3, 1280)  # 设置显示窗口宽度1280
cap.set(4, 720)  # 显示窗口高度720

pTime = 0  # 处理第一帧图像的起始时间


# （2）接收手部检测方法
detector = HandDetector(mode=False,  # 静态图模式，若为True，每一帧都会调用检测方法，导致检测很慢
                        maxHands=1,  # 最多检测几只手
                        detectionCon=0.8,  # 最小检测置信度
                        minTrackCon=0.5)  # 最小跟踪置信度

# 找到手掌间的距离和实际的手与摄像机之间的距离的映射关系
# x 代表手掌间的距离(像素距离)，y 代表手和摄像机之间的距离(cm)
x = [300, 245, 200, 170, 145, 130, 112, 103, 93, 87, 80, 75, 70, 67, 62, 59, 57]
y = [20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100]

# 绘图查看xy的对应关系
import matplotlib.pyplot as plt

plt.plot(x, y)
plt.xlabel('x')
plt.ylabel('y')
plt.title('reflection')

# 因此我们需要一个类似 y = AX^2 + BX + C 的方程来拟合
import numpy as np

coff = np.polyfit(x, y, 2)  # 构造二阶多项式方程
# coff中存放的是二阶多项式的系数 A,B,C

# （4）处理每一帧图像
while True:

    # 返回图像是否读取成功，以及读取的帧图像img
    success, img = cap.read()

    # （5）获取手部关键点信息
    # 检测手部信息，返回手部关键点信息hands字典，不绘制图像
    hands = detector.findHands(img, draw=False)

    # 如果检测到手的话hands字典就不为空
    if hands:
        # 获取检测框的信息(x,y,w,h)
        x, y, w, h = hands[0]['bbox']

        # 获取字典中的关键点信息，key为lmList
        lmList = hands[0]['lmList']  # hands[0]代表检测到的这只手的字典信息，hands是一个列表
        print('hands_landmarks:', lmList)

        # 获取食指根部'5'和小指根部'17'的坐标点
        # print(lmList[5][:2], lmList[17][:2])
        x1, y1 = lmList[5][:2]
        x2, y2 = lmList[17][:2]
        # print(x1,y1,x2,y2)
        # cv2.circle(img, (x1, y1), 15, (255, 0, 255), cv2.FILLED)
        # cv2.circle(img, (x2, y2), 15, (255, 0, 255), cv2.FILLED)
        cv2.line(img, (x1, y1), (x2, y2), (255, 0, 255), 10, cv2.FILLED)

        # 勾股定理计算关键点'5'和'17'之间的距离，并变成整型
        distance = int(math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2))
        print('distance between 5 and 17:', distance)

        # 拟合的二次多项式的系数保存在coff数组中，即掌间距离和手与相机间的距离的对应关系的系数
        A, B, C = coff

        # 得到像素距离转为实际cm距离的公式 y = Ax^2 + Bx + C
        distanceCM = A * distance ** 2 + B * distance + C
        print('distance CM:', distanceCM)

        # 把距离绘制在图像上，简化了cv2.putText()，
        cvzone.putTextRect(img, f'{(int(distanceCM))} cm', (x + 10, y - 10))

        # 绘制手部检测框
        cv2.rectangle(img, (x, y), (x + w, y + h), (0, 255, 0), 2)

    # （6）图像显示
    # 计算FPS值
    cTime = time.time()  # 处理一帧图像所需的时间
    fps = 1 / (cTime - pTime)
    pTime = cTime  # 更新处理下一帧的起始时间

    # 把fps值显示在图像上,img画板,显示字符串,显示的坐标位置,字体,字体大小,颜色,线条粗细
    cv2.putText(img, str(int(fps)), (50, 70), cv2.FONT_HERSHEY_PLAIN, 3, (255, 0, 0), 3)

    # 显示图像，输入窗口名及图像数据
    # cv2.namedWindow("img", 0)  # 窗口大小可手动调整
    cv2.imshow('img', img)
    if cv2.waitKey(20) & 0xFF == 27:  # 每帧滞留20毫秒后消失，ESC键退出
        break

# 释放视频资源
cap.release()
cv2.destroyAllWindows()