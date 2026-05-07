map_index_func = {1:'x',21:'log',22:'ceil',23:'tan',24:'cos',25:'sin',26:'add', 27:'arc_length', 28:'ceil', 29:'combinations',
                  30:'floor', 31:'integer_square_root', 32:'sum_of_series',
                  33: 'cos', 34: 'radians', 35: 'vol_cube', 36: 'vol_sphere',
                  37: 'vol_hemisphere', 38: 'vol_icosahedron', 39: 'abs_val',
                  40: 'surface_area_hemisphere', 41: 'surface_area_cube', 42: 'surface_area_sphere',
                  43: 'area_square', 44: 'area_circle', 45: 'dodecahedron_surface_area',
                  46: 'dodecahedron_volume', 47: 'gamma_recursive', 48: 'signum',
                  49: 'sylvester', 50: 'remove_digit', 51: 'decimal_to_negative_base_2',
                  52: 'number_of_divisors', 53: 'sum_of_digits', 54:'sum_of_divisors',
                  55:'double_factorial_recursive', 56:'double_factorial_iterative', 57:'factorial',
                  58:'factorial_recursive', 59:'exact_prime_factor_count', 60:'dynamic_lucas_number',
                  61:'num_digits', 62:'num_digits_fast', 63:'num_digits_faster',
                  64:'multiplicative_persistence', 65:'additive_persistence'}


def get_filename_from_path(filepath):
    """从文件路径提取文件名（不带扩展名）"""
    import os
    filename_with_ext = os.path.basename(filepath)
    filename = os.path.splitext(filename_with_ext)[0]
    return filename


def add_function_to_map_index(index, filepath):
    """从文件路径提取函数名，并添加到 map_index_func 字典

    Args:
        index (int): 要添加的索引
        filepath (str): Python 文件的绝对路径
    """
    func_name = get_filename_from_path(filepath)
    if index == 1:  # 如果 index 是 1，直接覆盖
        map_index_func[index] = func_name
        print(f"Successfully updated index {index} to '{func_name}' in map_index_func")
    elif index in map_index_func:  # 其他情况，检查是否已存在
        raise ValueError(f"Index {index} already exists in map_index_func")
    else:  # 如果 index 不存在，则添加
        map_index_func[index] = func_name
        print(f"Successfully added {func_name} with index {index} to map_index_func")

filepath = r'D:\1_Project\C#_Project_100\MetBench_101\MetBench\未开发的新功能\AutoMR-MT-Many-master-cos-100\AutoMR-MT-Many-master-cos-100\AutoMR-MT-Many-master\function\sin.py'  # 假设文件名是 "my_function"

add_function_to_map_index(1, filepath)


