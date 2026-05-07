from math import pi, pow

def vol_sphere(radius: float) -> float:
    """
    Calculate the Volume of a Sphere.
    Wikipedia reference: https://en.wikipedia.org/wiki/Sphere
    :return (4/3) * pi * r^3
    # >>> vol_sphere(5)
    # 523.5987755982989
    # >>> vol_sphere(1)
    # 4.1887902047863905
    # >>> vol_sphere(1.6)
    # 17.15728467880506
    # >>> vol_sphere(0)
    # 0.0
    # >>> vol_sphere(-1)
    Traceback (most recent call last):
        ...
    ValueError: vol_sphere() only accepts non-negative values
    """
    if radius < 0:
        raise ValueError("vol_sphere() only accepts non-negative values")
    # Volume is 4/3 * pi * radius cubed
    return 4 / 3 * pi * pow(radius, 3)