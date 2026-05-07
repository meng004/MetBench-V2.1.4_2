import os
import sys
import tempfile
from datetime import datetime
from io import BytesIO
import uuid  # 确保这行存在
from matplotlib import pyplot as plt
# c#/.Net交互用,这时请取消注释
latex_expression = sys.argv[1]

# latex_expression = r"y_1+y_2+y_3-y_4=4*y_5*y_6*y_7"
def LatexShow(latex_expression, size=(1.3, 0.5)):
    # 获取系统的临时文件夹路径
    temp_folder = tempfile.gettempdir()

    # 构建MetBench文件夹路径
    metbench_folder = os.path.join(temp_folder, "MetBench")

    # 构建MR_Image文件夹路径
    mr_image_folder = os.path.join(metbench_folder, "MR_Image")

    # 检查MR_Image文件夹是否存在
    if not os.path.exists(mr_image_folder):
        # 检查MetBench文件夹是否存在
        if not os.path.exists(metbench_folder):
            # 若MetBench文件夹不存在，创建MetBench文件夹
            os.makedirs(metbench_folder)
        # 创建MR_Image文件夹
        os.makedirs(mr_image_folder)

    directory = mr_image_folder
    # 在数学表达式前后添加符号，并在每个表达式之间添加换行
    formatted_expression = r'${}$\par'.format(latex_expression.replace(', ', r'$\par$'))
    # 测试输出latex原文格式
    # print(formatted_expression)
    # 创建一个包含数学表达式的文本框
    fig, ax = plt.subplots(figsize=size)
    text_obj = ax.text(0.5, 0.5, formatted_expression, size=20, ha='center', va='center', usetex=True)
    # 设置文本框和图像的颜色，并使用 RGBA 格式
    bg_color = (0.75, 0.75, 0.75, 0.0)  # 透明背景
    # text_color = (1.0, 1.0, 1.0, 1.0)  # 白色文本
    text_color = (0.0, 0.0, 0.0, 1.0)  # 黑色文本
    text_obj.set_bbox(dict(facecolor=bg_color, edgecolor=bg_color))
    fig.set_facecolor(bg_color)
    text_obj.set_color(text_color)
    # 隐藏坐标轴
    ax.axis('off')
    # 生成独一无二的文件名（使用日期和时间戳）
    timestamp = datetime.now().strftime("%Y%m%d%H%M%S")
    random_str = uuid.uuid4().hex[:8]  # 取UUID前8位
    file_name = f"output_{timestamp}_{random_str}"
    # 将图像保存到内存中
    img_buffer = BytesIO()
    plt.savefig(img_buffer, format='png', bbox_inches='tight', transparent=True)
    img_buffer.seek(0)
    save_path = os.path.join(directory, file_name + ".png")
    with open(save_path, 'wb') as f:
        f.write(img_buffer.getvalue())
        print(os.path.abspath(save_path))
# 测试组
# 0一次输入仅一个表达式
# X_{1}=-X_{2}
# 1加 减 乘 除 分数 绝对值 对数
# X_{2}=(X_{1}-1)*(X_{1}+3)+\frac{X_{1}^{2}}{a^{2}}+a/b+\pi, X_{1}= \ln | X_{1} | +C
# 2三角函数 阿尔法 贝塔 上下标 根号
# X_{1}=2 \cos \frac{\alpha + \beta}{2}\sin \frac{\alpha + \beta}{2}, X_{2} = \sqrt[n]{X_{1}}
latex_expression0 = r"X_{1}=-X_{2}"
# latex_expression1 = r"X_{2}=(X_{1}-1)*(X_{1}+3)+\frac{X_{1}^{2}}{a^{2}}+a/b+\pi, X_{1}= \ln | X_{1} | +C"
# latex_expression2 = r"X_{1}=2 \cos \frac{\alpha + \beta}{2}\sin \frac{\alpha + \beta}{2}, X_{2} = \sqrt[n]{X_{1}}, X_{1}=-X_{2}"
# LatexShow(latex_expression0)
# LatexShow(latex_expression1)
LatexShow(latex_expression)