from math import pi

def area_circle(radius: float) -> float:
    """
    Calculate the area of a circle.
    #
    # >>> area_circle(20)
    # 1256.6370614359173
    # >>> area_circle(1.6)
    # 8.042477193189871
    # >>> area_circle(0)
    # 0.0
    # >>> area_circle(-1)
    Traceback (most recent call last):
        ...
    ValueError: area_circle() only accepts non-negative values
    """
    if radius < 0:
        raise ValueError("area_circle() only accepts non-negative values")
    return pi * radius**2