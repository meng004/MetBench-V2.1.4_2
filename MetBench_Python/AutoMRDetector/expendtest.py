import os



input_program_filename = None  # 全局变量初始化
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
#
# def set_filename_from_frontend(filename):
#     """安全接收前端文件名，并确保function文件夹中存在该文件"""
#     global input_program_filename
#
#     # 检查1：基本验证
#     if not filename or not isinstance(filename, str) or not filename.strip():
#         print("错误：文件名无效！")
#         return False
#
#     # 获取当前脚本所在目录
#     current_dir = os.path.dirname(os.path.abspath(__file__))
#     function_dir = os.path.join(current_dir, "function")
#
#     # 确保function目录存在
#     os.makedirs(function_dir, exist_ok=True)
#
#     # 构建目标文件路径
#     target_path = os.path.join(function_dir, filename)
#
#     # 检查文件是否存在，不存在则创建
#     if not os.path.exists(target_path):
#         print(f"警告：文件 {filename} 在function目录中不存在，将创建空文件")
#         try:
#             with open(target_path, 'w') as f:
#                 f.write("")  # 创建空文件
#             print(f"已创建空文件: {target_path}")
#         except Exception as e:
#             print(f"创建文件失败: {e}")
#             return False
#
#     # 所有检查通过后赋值全局变量
#     input_program_filename = filename
#     print(f"文件名已验证并更新为: {filename}")
#     return True
#
# # 用于去除文件名后缀
# def get_filename_without_extension():
#     """
#     检查全局变量input_program_filename的值
#     如果存在且非空，返回去掉.py后缀的文件名
#     如果不存在或为空，返回None
#     """
#     global input_program_filename
#
#     if input_program_filename and isinstance(input_program_filename, str):
#         # 去掉.py后缀（如果有的话）
#         if input_program_filename.endswith('.py'):
#             return input_program_filename[:-3]
#         return input_program_filename
#     return None
#
#
# def update_index_1_with_filename():
#     """
#     Updates the value at index 1 in map_index_func with the filename without extension.
#     If get_filename_without_extension() returns None, keeps the original value ('x').
#     """
#     global map_index_func
#
#     # Get the filename without extension
#     filename = get_filename_without_extension()
#
#     # Update the dictionary only if filename is not None
#     if filename is not None:
#         map_index_func[1] = filename
#         print(f"Updated index 1 with: {filename}")
#     else:
#         print("No filename available, keeping original value 'x'")
#
#     return map_index_func
#
#
#
#
#
# # 测试用例1：有效文件名
# print("测试1：有效文件名")
# result = set_filename_from_frontend("sin.py")
# a = input_program_filename
# update_index_1_with_filename()
# print("After update:", map_index_func[1])  # Should be 'sin'
# print(f"结果: {result}")
# print(f"去掉扩展名的文件名: {get_filename_without_extension()}\n")
#
# # 测试用例2：检查未设置的情况
# print("测试2：未设置文件名")
# input_program_filename = None
# update_index_1_with_filename()
# print("After update:", map_index_func[1])  # Should be 'sin'
# print(f"去掉扩展名的文件名: {get_filename_without_extension()}\n")
#
# # 测试用例3：文件名没有.py后缀
# print("测试3：文件名没有.py后缀")
# input_program_filename = "cos"
# update_index_1_with_filename()
# print("After update:", map_index_func[1])  # Should be 'sin'
# print(f"去掉扩展名的文件名: {get_filename_without_extension()}\n")

frontend_input_ranges = None  # 存储多个范围的列表
input_param_count = 1  # 默认参数个数
frontend_input_datatypes = None

def set_frontend_input_range(range_str, param_count=1):
    """
    接收前端传入的输入范围字符串，支持单参数和多参数格式
    返回值始终是嵌套列表格式，如 [[min,max]] 或 [[min1,max1],[min2,max2]]
    """
    global frontend_input_ranges, input_param_count

    input_param_count = param_count
    stripped = range_str.replace(" ", "")

    try:
        if param_count == 1:
            # 处理单参数格式 "(min,max)"
            if not (stripped.startswith('(') and stripped.endswith(')')):
                raise ValueError("单参数范围格式应为'(min,max)'")

            min_val, max_val = map(int, stripped[1:-1].split(','))
            frontend_input_ranges = [[min_val, max_val]]

        else:
            # 处理多参数格式 "((min1,max1),(min2,max2))"
            if not (stripped.startswith('((') and stripped.endswith('))')):
                raise ValueError(f"多参数范围格式应为'((min1,max1),(min2,max2))'")

            parts = stripped[2:-2].split('),(')
            if len(parts) != param_count:
                raise ValueError(f"提供的范围数量({len(parts)})与参数个数({param_count})不匹配")

            frontend_input_ranges = []
            for part in parts:
                min_val, max_val = map(int, part.split(','))
                frontend_input_ranges.append([min_val, max_val])

    except ValueError as e:
        raise ValueError(f"无效的范围格式: {str(e)}") from e

    # 验证所有范围
    for r in frontend_input_ranges:
        if r[0] >= r[1]:
            raise ValueError(f"范围最小值{r[0]}不能大于等于最大值{r[1]}")


def set_frontend_input_datatype(datatype_str, param_count=1):
    """
    接收前端传入的数据类型字符串，支持单参数和多参数格式
    格式示例：
    - 单参数: "int" 或 "float"
    - 多参数: "(int,float)" 或 "[int,float]"
    """
    global frontend_input_datatypes, input_param_count

    input_param_count = param_count
    stripped = datatype_str.replace(" ", "")

    try:
        if param_count == 1:
            # 单参数情况: "int" 或 "float"
            if stripped not in ["int", "float"]:
                raise ValueError("单参数数据类型应为 'int' 或 'float'")
            frontend_input_datatypes = [eval(stripped)]

        else:
            # 多参数情况: "(type1,type2)" 或 "[type1,type2]"
            if not (stripped.startswith(('(', '[')) and stripped.endswith((')', ']'))):
                raise ValueError(f"多参数格式应为 '(type1,type2)' 或 '[type1,type2]'")

            parts = stripped[1:-1].split(',')
            if len(parts) != param_count:
                raise ValueError(f"提供的数据类型数量({len(parts)})与参数个数({param_count})不匹配")

            frontend_input_datatypes = []
            for part in parts:
                if part not in ["int", "float"]:
                    raise ValueError(f"无效的数据类型: {part} (必须是 'int' 或 'float')")
                frontend_input_datatypes.append(eval(part))

    except (ValueError, SyntaxError) as e:
        raise ValueError(f"无效的数据类型格式: {str(e)}") from e

def get_input_range(func_index):
    """
    获取函数输入范围，返回值始终是嵌套列表格式
    例如: [[-10,10]] 或 [[-10,10], [-10,10]]
    """
    # 原有逻辑保持不变...
    if func_index in [26, 27, 29]:
        return [[-10, 10], [-10, 10]]
    elif func_index in [31, 50, 51, 53, 60, 61, 62, 63]:
        return [[-10, 10]]
    elif func_index in [32]:
        return [[-10, 10], [-10, 10], [-10, 10]]
    elif func_index in [22, 23, 24, 25, 28, 30, 33, 34, 39, 48]:
        return [[-10, 10]]
    elif func_index in [35, 36, 37, 38, 40, 41, 42, 43, 44, 45, 46, 47]:
        return [[0, 20]]
    elif func_index in [49, 55, 56, 57, 58, 64, 65]:
        return [[0, 20]]
    elif func_index in [21, 52, 54, 59]:
        return [[1, 20]]
    elif func_index in [1]:
        if frontend_input_ranges is None:
            default_range = [-20, 20]
            return [default_range.copy() for _ in range(input_param_count)]
        # 返回拷贝以保持格式一致
        return [r.copy() for r in frontend_input_ranges]
def get_input_datatype(func_index):
    """
    获取函数输入的数据类型，返回值始终是列表格式
    例如: [int] 或 [float, int]
    当未设置前端数据类型时，默认返回int类型
    """
    # 原有逻辑保持不变
    if func_index in [26, 27, 29]:
        return [int, int]
    elif func_index in [31, 49, 50, 51, 52, 53, 54, 55, 56,
                        57, 58, 59, 60, 61, 62, 63, 64, 65]:
        return [int]
    elif func_index in [32]:
        return [int, int, int]
    elif func_index in [21,22,23,24,25,28, 30, 33, 34, 35, 36, 37, 38, 39,
                        40, 41, 42, 43, 44, 45, 46, 47, 48]:
        return [float]
    elif func_index in [1]:
        if frontend_input_datatypes is None:
            # 返回默认int类型，数量根据input_param_count决定
            return [int for _ in range(input_param_count)]
        return frontend_input_datatypes.copy()  # 返回拷贝避免外部修改

# 测试用例
if __name__ == "__main__":
    # input_param_count = 1
    # print(get_input_range(1))  # 输出: [[-20, 20]]
    #
    # input_param_count = 2
    # print(get_input_range(1))  # 输出: [[-20, 20], [-20, 20]]
    # # 测试单参数（返回值格式应为[[min,max]]）
    # set_frontend_input_range("(10, 20)", 1) 单参数范围(10,20)
    # print("单参数范围:", get_input_range(1))  # 输出: [[10, 20]]
    # a = get_input_range(22)
    # print("单参数范围:", get_input_range(22))  # 输出: [[10, 20]]
    # # 测试双参数（返回值格式应为[[min1,max1],[min2,max2]]）
    # set_frontend_input_range("((20,30),(30,40))", 2) 多参数范围如((20,30),(30,40))
    # print("双参数范围:", get_input_range(1))  # 输出: [[20, 30], [30, 40]]
    #
    # a = frontend_input_ranges
    # # 测试与预设范围的格式一致性
    # print("预设范围26:", get_input_range(26))  # 输出: [[-10, 10], [-10, 10]]
    # print("预设范围31:", get_input_range(31))  # 输出: [[-10, 10]]
    # 测试未设置前端类型时的默认行为
    print("测试默认int类型:")
    input_param_count = 1
    print("单参数默认类型:", get_input_datatype(1))  # 输出: [<class 'int'>]

    input_param_count = 2
    print("双参数默认类型:", get_input_datatype(1))  # 输出: [<class 'int'>, <class 'int'>]

    # 其他测试保持不变...
    # 测试单参数
    print("\n测试单参数:")
    set_frontend_input_datatype("float", 1)
    print("单参数数据类型:", get_input_datatype(1))  # 输出: [<class 'float'>]

    # 测试双参数
    print("\n测试双参数:")
    set_frontend_input_datatype("(int,float)", 2)
    print("双参数数据类型:", get_input_datatype(1))  # 输出: [<class 'int'>, <class 'float'>]
    a = get_input_datatype(22)
    print("双参数数据类型:", get_input_datatype(22))  # 输出: [<class 'int'>, <class 'float'>]