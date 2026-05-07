import math
def arccos(x):
    if abs(x) > 1:
        return "Invalid input. x should be between -1 and 1."
    # 使用反余弦的数学公式计算arccos(x)
    radians = math.pi/2 - math.atan(x / math.sqrt(1 - x*x))
    return radians
