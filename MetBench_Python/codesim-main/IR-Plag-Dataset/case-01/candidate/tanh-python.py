import math
def tanh(x):
    if x > 20:
        return 1
    elif x < -20:
        return -1
    else:
        return (2 / (1 + math.exp(-2 * x))) - 1

