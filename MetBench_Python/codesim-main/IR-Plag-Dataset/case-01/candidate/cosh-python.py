import numpy as np
def cosh(x, n_terms=30):
    result = 1  # 初始化结果为1
    numerator = 1  # 分子
    denominator = 1  # 分母
    for i in range(1, n_terms):
        numerator *= x * x  # 更新分子
        denominator *= (2 * i) * (2 * i - 1)  # 更新分母
        term = numerator / denominator  # 计算每一项
        result += term  # 加上每一项
    return result
