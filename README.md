# HandVirtualInteraction
基于Mediapipe和Unity的手部捕捉虚拟交互系统
主要研究了触发性手势识别的交互实际应用
具体识别流程是根据自己的思考和总结的思路实现的
目前已训练可进行的操作有（包含左右手）：拇指向上滑动、拇指向下滑动、拇指向左滑动、拇指向右滑动、手掌向左滑动、手掌向右滑动、响指（响指操作为实验性手势，可用但并不做演示）

项目演示视频链接：
【毕设演示记录-基于Mediapipe和Unity的手部捕捉虚拟交互系统】 https://www.bilibili.com/video/BV1R14y1Z77b/?share_source=copy_web&vd_source=170b86cbffdf4bfae7d61a79544c1d48


识别算法是基于Lstm的实际应用，所用到的模型是自己采集数据并训练的
算法方面的启发来自：https://blog.csdn.net/kalakalabala/article/details/124081529

人物模型驱动方法是我在学习了两三种方法后最终确定的
人物模型驱动启发来自：https://huailiang.github.io/blog/2020/3dpose/utm_source=wechat_session&utm_medium=social&utm_oi=988369452974518272
