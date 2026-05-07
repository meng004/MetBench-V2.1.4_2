def add(first: int, second: int) -> int:
    """
    Implementation of addition of integer

    Examples:
    # >>> add(3, 5)
    # 8
    # >>> add(13, 5)
    # 18
    # >>> add(-7, 2)
    # -5
    # >>> add(0, -7)
    # -7
    # >>> add(-321, 0)
    # -321
    """
    while second != 0:
        c = first & second
        first ^= second
        second = c << 1
    return first

# print(add(-7, 2))