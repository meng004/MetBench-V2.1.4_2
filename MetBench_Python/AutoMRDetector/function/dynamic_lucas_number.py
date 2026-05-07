def dynamic_lucas_number(n_th_number: int) -> int:
    """
    Returns the nth lucas number
    # >>> dynamic_lucas_number(1)
    # 1
    # >>> dynamic_lucas_number(20)
    # 15127
    # >>> dynamic_lucas_number(0)
    # 2
    # >>> dynamic_lucas_number(25)
    # 167761
    # >>> dynamic_lucas_number(-1.5)
    Traceback (most recent call last):
        ...
    TypeError: dynamic_lucas_number accepts only integer arguments.
    """
    if not isinstance(n_th_number, int):
        raise TypeError("dynamic_lucas_number accepts only integer arguments.")
    a, b = 2, 1
    for _ in range(n_th_number):
        a, b = b, a + b
    return a