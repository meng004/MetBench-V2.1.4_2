def vol_icosahedron(tri_side: float) -> float:
    """Calculate the Volume of an Icosahedron.
    Wikipedia reference: https://en.wikipedia.org/wiki/Regular_icosahedron

    # >>> from math import isclose
    # >>> isclose(vol_icosahedron(2.5), 34.088984228514256)
    # True
    # >>> isclose(vol_icosahedron(10), 2181.694990624912374)
    # True
    # >>> isclose(vol_icosahedron(5), 272.711873828114047)
    # True
    # >>> isclose(vol_icosahedron(3.49), 92.740688412033628)
    # True
    # >>> vol_icosahedron(0)
    # 0.0
    # >>> vol_icosahedron(-1)
    # Traceback (most recent call last):
    #     ...
    # ValueError: vol_icosahedron() only accepts non-negative values
    # >>> vol_icosahedron(-0.2)
    Traceback (most recent call last):
        ...
    ValueError: vol_icosahedron() only accepts non-negative values
    """
    if tri_side < 0:
        raise ValueError("vol_icosahedron() only accepts non-negative values")
    return tri_side**3 * (3 + 5**0.5) * 5 / 12