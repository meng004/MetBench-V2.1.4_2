import math
def arcsinh(x):
    if x == 0:
        return 0
    # 使用log函数计算arcsinh(x)
    return math.log(x + math.sqrt(x**2 + 1))
