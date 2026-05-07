import math
def arccosh(x):
    if x < 1:
        return float('nan')
    result = math.log(x + math.sqrt(x**2 - 1))
    return result
