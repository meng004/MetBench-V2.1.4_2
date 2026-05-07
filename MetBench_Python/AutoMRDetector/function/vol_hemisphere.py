from math import pi, pow

def vol_hemisphere(radius: float) -> float:
    """Calculate the volume of a hemisphere
    Wikipedia reference: https://en.wikipedia.org/wiki/Hemisphere
    Other references: https://www.cuemath.com/geometry/hemisphere
    :return 2/3 * pi * radius^3
    # >>> vol_hemisphere(1)
    # 2.0943951023931953
    # >>> vol_hemisphere(7)
    # 718.377520120866
    # >>> vol_hemisphere(1.6)
    # 8.57864233940253
    # >>> vol_hemisphere(0)
    # 0.0
    # >>> vol_hemisphere(-1)
    Traceback (most recent call last):
        ...
    ValueError: vol_hemisphere() only accepts non-negative values
    """
    if radius < 0:
        raise ValueError("vol_hemisphere() only accepts non-negative values")
    # Volume is radius cubed * pi * 2/3
    return pow(radius, 3) * pi * 2 / 3