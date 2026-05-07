import math
def arcsin(x):
    if abs(x) > 1:
        return "Invalid input. x should be between -1 and 1."
    # 使用反正弦的数学公式计算arcsin(x)
    radians = math.atan2(x, math.sqrt(1 - x*x))
    return radians
