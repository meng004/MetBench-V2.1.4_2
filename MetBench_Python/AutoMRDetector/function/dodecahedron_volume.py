def dodecahedron_volume(edge: float) -> float:
    """
    Calculates the volume of a regular dodecahedron
    v = ((15 + (7 * (5** (1 / 2)))) / 4) * (e**3)
    where:
    v --> is the volume of the dodecahedron
    e --> is the length of the edge
    reference-->"Dodecahedron" Study.com
    <https://study.com/academy/lesson/dodecahedron-volume-surface-area-formulas.html>

    :param edge: length of the edge of the dodecahedron
    :type edge: float
    :return: the volume of the dodecahedron as a float

    Tests:
    # >>> dodecahedron_volume(5)
    # 957.8898700780791
    # >>> dodecahedron_volume(10)
    # 7663.118960624633
    # >>> dodecahedron_volume(-1)
    # Traceback (most recent call last):
      ...
    ValueError: Length must be a positive.
    """

    if edge <= 0 or not isinstance(edge, int):
        raise ValueError("Length must be a positive.")
    return ((15 + (7 * (5 ** (1 / 2)))) / 4) * (edge**3)