import numpy as np
import get_function
import os
import json
import os

import sys
# map_index_func = {1:'abs', 2:'arcvos', 3:'arccosh', 4:'arcsin', 5:'arcsinh', 6:'arctan', 7:'arctan2', 8:'arctanh', 9:'ceil', 11:'cos', 12:'cosh', 12:'exp', 13:'floor', 14:'hypot', 15:'log', 15:'log1p', 17:'log10', 18:'amax', 19:'amin', 20:'round', 12:'sin', 12:'sinh', 23:'sqrt', 24:'tan', 25:'tanh'}
# map_index_func = {1:'abs', 2:'arcvos', 3:'arccosh', 4:'arcsin', 5:'arcsinh', 6:'arctan', 7:'arctan2',
#                   8:'arctanh', 9:'ceil', 10:'cos', 11:'cosh', 12:'exp', 13:'floor', 14:'hypot', 15:'log',
#                   16:'log1p', 17:'log10', 18:'amax', 19:'amin', 20:'round', 21:'sin', 22:'sinh', 23:'sqrt',
#                   24:'tan', 25:'tanh'}

# 全局变量，用于接受前端输入待识别程序文件名称 初始化为 None 表示未接收到文件名
input_program_filename = None
input_param_count = 1  # 默认函数输入参数个数
frontend_input_ranges = None  # 存储多个范围的列表
frontend_input_datatypes = None #参数类型

CONFIG_FILE = "config.json"


def load_config():
    """加载配置文件"""
    if not os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, 'w') as f:
            json.dump({
                "input_program_filename": None,
                "input_param_count": 1,
                "frontend_input_ranges": None,
                "frontend_input_datatypes": None
            }, f)

    with open(CONFIG_FILE, 'r') as f:
        return json.load(f)


def save_config(config):
    """保存配置文件"""
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=4)

# 接收前端传入待识别函数的文件名称
def set_filename_from_frontend():
    """安全接收前端文件名，并确保function文件夹中存在该文件"""
    """更新配置文件中的文件名"""
    config = load_config()
    filename = config["input_program_filename"]
    # 检查1：基本验证
    if not filename or not isinstance(filename, str) or not filename.strip():
        print("错误：文件名无效！")
        return None

    # 获取当前脚本所在目录
    current_dir = os.path.dirname(os.path.abspath(__file__))
    function_dir = os.path.join(current_dir, "function")

    # 确保function目录存在
    os.makedirs(function_dir, exist_ok=True)

    # 构建目标文件路径
    target_path = os.path.join(function_dir, filename)

    # 检查文件是否存在，不存在则创建
    if not os.path.exists(target_path):
        print(f"警告：文件 {filename} 在function目录中不存在，将创建空文件")
        try:
            with open(target_path, 'w') as f:
                f.write("")  # 创建空文件
            print(f"已创建空文件: {target_path}")
        except Exception as e:
            print(f"创建文件失败: {e}")
            return None
    # 所有检查通过后赋值全局变量
    # print(f"文件名已验证并更新为: {filename}")
    # config["input_program_filename"] = filename
    # save_config(config)

    return filename

# 用于去除文件名后缀
def get_filename_without_extension():
    """
    检查全局变量input_program_filename的值
    如果存在且非空，返回去掉.py后缀的文件名
    如果不存在或为空，返回None
    """
    config = load_config()
    filename = config["input_program_filename"]


    if filename and isinstance(filename, str):
        # 去掉.py后缀（如果有的话）
        if filename.endswith('.py'):
            return filename[:-3]
        return filename
    return None


# 从前端接收参数个数并保存到全局变量
def set_input_param_count_from_frontend():
    """
    从前端接收参数个数并保存到全局变量
    参数: param_count (int) - 前端传入的参数个数
    返回: bool - 设置是否成功
    """
    config = load_config()
    param_count = config["input_param_count"]
    # 参数验证
    if not isinstance(param_count, int) or param_count < 0:
        print("错误：参数个数必须是正整数！")
        return None
    #
    # print(f"已设置全局参数个数为: {param_count}")
    # config["input_param_count"] = param_count
    # save_config(config)
    return param_count

def set_frontend_input_range():
    """
    接收前端传入的输入范围字符串，支持单参数和多参数格式 单参数范围(10,20) 多参数范围如((20,30),(30,40))
    返回值始终是嵌套列表格式，如 [[min,max]] 或 [[min1,max1],[min2,max2]]
    """
    config = load_config()
    range_str = config["frontend_input_ranges"]
    param_count = config["input_param_count"]
    # 检查输入范围是否为空
    if not range_str:
        print("错误：输入范围为空！")
        return None
    stripped = range_str.replace(" ", "")

    try:
        if param_count == 1:
            # 处理单参数格式 "(min,max)"
            if not (stripped.startswith('(') and stripped.endswith(')')):
                raise ValueError("单参数范围格式应为'(min,max)'")

            min_val, max_val = map(int, stripped[1:-1].split(','))
            range_str = [[min_val, max_val]]

        else:
            # 处理多参数格式 "((min1,max1),(min2,max2))"
            if not (stripped.startswith('((') and stripped.endswith('))')):
                raise ValueError(f"多参数范围格式应为'((min1,max1),(min2,max2))'")

            parts = stripped[2:-2].split('),(')
            if len(parts) != param_count:
                raise ValueError(f"提供的范围数量({len(parts)})与参数个数({param_count})不匹配")

            range_str = []
            for part in parts:
                min_val, max_val = map(int, part.split(','))
                range_str.append([min_val, max_val])

    except ValueError as e:
        raise ValueError(f"无效的范围格式: {str(e)}") from e


    # 验证所有范围
    for r in range_str:
        if r[0] >= r[1]:
            raise ValueError(f"范围最小值{r[0]}不能大于等于最大值{r[1]}")

    return range_str
def set_frontend_input_datatype():
    """
    接收前端传入的数据类型字符串，支持单参数和多参数格式
    格式示例：
    - 单参数: "int" 或 "float"
    - 多参数: "(int,float)" 或 "[int,float]"
    """

    config = load_config()
    datatype_str = config["frontend_input_datatypes"]
    param_count = config["input_param_count"]
    stripped = datatype_str.replace(" ", "")

    try:
        if param_count == 1:
            # 单参数情况: "int" 或 "float"
            if stripped not in ["int", "float"]:
                raise ValueError("单参数数据类型应为 'int' 或 'float'")
            datatype_str = [eval(stripped)]
            # datatype_str = stripped

        else:
            # 多参数情况: "(type1,type2)" 或 "[type1,type2]"
            if not (stripped.startswith(('(', '[')) and stripped.endswith((')', ']'))):
                raise ValueError(f"多参数格式应为 '(type1,type2)' 或 '[type1,type2]'")

            parts = stripped[1:-1].split(',')
            if len(parts) != param_count:
                raise ValueError(f"提供的数据类型数量({len(parts)})与参数个数({param_count})不匹配")

            datatype_str = []
            for part in parts:
                if part not in ["int", "float"]:
                    raise ValueError(f"无效的数据类型: {part} (必须是 'int' 或 'float')")
                datatype_str.append(eval(part))
                # datatype_str.append(part)

    except (ValueError, SyntaxError) as e:
        raise ValueError(f"无效的数据类型格式: {str(e)}") from e
    # config["frontend_input_datatypes"] = datatype_str
    # save_config(config)
    return datatype_str

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

# def update_custom_function():
#     name = update_index_1_with_filename()
#     if not isinstance(name, str):
#         raise TypeError("函数名必须是字符串")
#     map_index_func[1] = name

def get_function_map():
    update_index_1_with_filename()
    return map_index_func.copy()

def update_index_1_with_filename():
    """
    Updates the value at index 1 in map_index_func with the filename without extension.
    If get_filename_without_extension() returns None, keeps the original value ('x').
    """
    global map_index_func

    # Get the filename without extension
    filename = get_filename_without_extension()

    # Update the dictionary only if filename is not None
    if filename is not None:
        map_index_func[1] = filename
        # print(f"Updated index 1 with: {filename}")
    else:
        print("No filename available, keeping original value 'x'")

    return map_index_func

# which programs to infer MRs from
# func_indices = list(range(33, 34))
# func_indices = list(range(35, 65))
func_indices = list(range(1, 65))
# func_indices = [12]

# which type of MRs to infer: NOI_MIR_MOR_DIR_DOR.
# NOI: number of involved inputs
# MIR, MOR: mode of input and output relations. 1-equal, 2-greaterthan, 3-lessthan
# DIR, DOR: degrees of input and output relations. 1-linear, 2-quadratic, etc.
# parameters_collection = ["2_1_1_1_1", "2_1_1_1_2", "2_1_1_1_3", "3_1_1_1_1", "3_1_1_1_2", "2_1_2_1_1", "2_1_3_1_1", "2_2_1_1_1", "2_3_1_1_1", "2_2_2_1_1", "2_2_3_1_1", "2_3_2_1_1", "2_3_3_1_1"]
parameters_collection = ["2_1_1_1_1"]

# # python外部
# module_path="sin.py"
# function= get_function.generate_function(module_path)

# program to infer MR from
# user can encapsulate his program under test here and assign it a func_index, which can be used to faciliate processing for multiple programs in a batch
# i is the array containing the input arguments (note that unary input is treated as a special case of multi-variate input)
# o is the array containing the returning results (note that unary output is treated as a special case of multi-variate output)
# func_index is assigned to facilitate batch processing for multiple programs
def program(i, func_index):
    # if func_index == 1:
    #     o = np.abs(i)
    # elif func_index == 2:
    #     o = np.arccos(i)
    # elif func_index == 3:
    #     o = np.arccosh(i)
    # elif func_index == 4:
    #     o = np.arcsin(i)
    # elif func_index == 5:
    #     o = np.arcsinh(i)
    # elif func_index == 6:
    #     o = np.arctan(i)
    # elif func_index == 7:
    #     o = np.arctan2(i[0], i[1])
    # elif func_index == 8:
    #     o = np.arctanh(i)
    # elif func_index == 9:
    #     o = np.ceil(i)
    # elif func_index == 10:
    #     o = np.cos(i)
    # elif func_index == 11:
    #     o = np.cosh(i)
    # elif func_index == 12:
    #     o = np.exp(i)
    # elif func_index == 13:
    #     o = np.floor(i)
    # elif func_index == 14:
    #     o = np.hypot(i[0], i[1])
    # elif func_index == 15:
    #     o = np.log(i)
    # elif func_index == 16:
    #     o = np.log(i + 1)
    # elif func_index == 17:
    #     o = np.log10(i)
    # elif func_index == 18:
    #     o = np.amax(i)
    # elif func_index == 19:
    #     o = np.amin(i)
    # elif func_index == 20:
    #     o = np.round(i)
    # elif func_index == 21:
    #     o = np.sin(i)
    # elif func_index == 22:
    #     o = np.sinh(i)
    # elif func_index == 23:
    #     o = np.sqrt(i)
    # elif func_index == 24:
    #     o = np.tan(i)
    # elif func_index == 25:
    #     o = np.tanh(i)
    # elif func_index == 26:
    #     # 外部sin.py
    #     module_path = "function/sin.py"
    #     function = get_function.generate_function(module_path)
    #     value=[]
    #     value.append(i)
    #     result= get_function.calculate_value_from_function(function, value)
    #     o=result
    # elif func_index == 27:
    #     # 外部amin.py
    #     module_path = "function/amin.py"
    #     function = get_function.generate_function(module_path)
    #     value=[]
    #     value.append(i[0])
    #     # print("1---",value)
    #     value.append(i[1])
    #     # print("2---",value)
    #     result= get_function.calculate_value_from_function(function, value)
    #     # print("result:",result)
    #     o=result
    if func_index == 21:
        o = np.log(i)
    elif func_index == 22:
        o = np.ceil(i)
    elif func_index == 23:
        o = np.tan(i)
    elif func_index == 24:
        o = np.cos(i)
    elif func_index == 25:
        o = np.sin(i)
    elif func_index == 26:
        module_path = "function/add.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i[0])
        # print("1---",value)
        value.append(i[1])
        # print("2---",value)
        result= get_function.calculate_value_from_function(function, value)
        # print("result:",result)
        o=result
    elif func_index == 27:
        module_path = "function/arc_length.py"
        function = get_function.generate_function(module_path)
        value=[]
        # 这里传入的是一个数组
        value.append(i[0])
        value.append(i[1])
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 28:
        module_path = "function/ceil.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 29:
        module_path = "function/combinations.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i[0])
        # print("1---",value)
        value.append(i[1])
        # print("2---",value)
        result= get_function.calculate_value_from_function(function, value)
        # print("result:",result)
        o=result
    elif func_index == 30:
        module_path = "function/floor.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 31:
        module_path = "function/integer_square_root.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 32:
        module_path = "function/sum_of_series.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i[0])
        # print("1---",value)
        value.append(i[1])
        # print("2---",value)
        value.append(i[2])
        result= get_function.calculate_value_from_function(function, value)
        # print("result:",result)
        o=result
    # elif func_index == 33:
    #     module_path = "function/vol_cube.py"
    #     function = get_function.generate_function(module_path)
    #     value=[]
    #     value.append(i)
    #     result= get_function.calculate_value_from_function(function, value)
    #     o=result
    elif func_index == 34:
        module_path = "function/radians.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 35:
        module_path = "function/vol_cube.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 36:
        module_path = "function/vol_sphere.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 37:
        module_path = "function/vol_hemisphere.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 38:
        module_path = "function/vol_icosahedron.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 39:
        module_path = "function/abs_val.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 40:
        module_path = "function/surface_area_hemisphere.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 41:
        module_path = "function/surface_area_cube.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 42:
        module_path = "function/surface_area_sphere.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 43:
        module_path = "function/area_square.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 44:
        module_path = "function/area_circle.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 45:
        module_path = "function/dodecahedron_surface_area.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 46:
        module_path = "function/dodecahedron_volume.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 47:
        module_path = "function/gamma_recursive.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 48:
        module_path = "function/signum.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 49:
        module_path = "function/sylvester.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 50:
        module_path = "function/remove_digit.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 51:
        module_path = "function/decimal_to_negative_base_2.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 52:
        module_path = "function/number_of_divisors.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        o=result
    elif func_index == 53:
        module_path = "function/sum_of_digits.py"
        function = get_function.generate_function(module_path)
        value=[]
        value.append(i)
        result= get_function.calculate_value_from_function(function, value)
        # print("计算结果：", result)  # 补充打印语句
        o=result
    elif func_index == 54:
        module_path = "function/sum_of_divisors.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        # print("计算结果：", result)  # 补充打印语句
        o = result
    elif func_index == 55:
        module_path = "function/double_factorial_recursive.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 56:
        module_path = "function/double_factorial_iterative.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 57:
        module_path = "function/factorial.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 58:
        module_path = "function/factorial_recursive.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 59:
        module_path = "function/exact_prime_factor_count.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 60:
        module_path = "function/dynamic_lucas_number.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 61:
        module_path = "function/num_digits.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 62:
        module_path = "function/num_digits_fast.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 63:
        module_path = "function/num_digits_faster.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 64:
        module_path = "function/multiplicative_persistence.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 65:
        module_path = "function/additive_persistence.py"
        function = get_function.generate_function(module_path)
        value = []
        value.append(i)
        result = get_function.calculate_value_from_function(function, value)
        o = result
    elif func_index == 1:
        # module_path = "function/additive_persistence.py"
        config = load_config()
        filename = set_filename_from_frontend()
        module_path = f"function/{filename}"
        function = get_function.generate_function(module_path)
        value = []
        if hasattr(i, '__len__'):  # 判断是否可获取长度（如列表、元组、NumPy数组）
            if len(i) == 1:
                value.append(i[0])  # 单元素直接追加
            else:
                value.extend(i)  # 多元素展开追加
        else:
            value.append(i)  # 非迭代类型直接追加
        result = get_function.calculate_value_from_function(function, value)
        o = result
    return o

# the number of elements of the input for the programs
def getNEI(func_index):
    # if func_index in [7, 14, 18, 19, 27]:
    # /////////////
    # if func_index in [26, 27, 29]:
    #     return 2
    # elif func_index in [32]:
    #     return 3
    # elif func_index in[28, 30, 31, 33, 34]:
    #     return 1
    # ////////////
    if func_index in [26, 27, 29]:
        return 2
    elif func_index in [32]:
        return 3
    elif func_index in [21,22,23,24,25,28, 30, 31, 33, 34, 35, 36, 37, 38, 39,
                        40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
                        50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
                        60, 61, 62, 63, 64, 65]:
        return 1
    elif func_index in [1]:
        # 如果全局变量已设置则返回，否则返回默认值1
        config = load_config()
        input_param_count = set_input_param_count_from_frontend()
        if input_param_count is not None:
            return input_param_count
        return 1  # 默认值
# the number of elements of the output for the programs
def getNEO(func_index):
    return 1

# domain for each element of the input
# QUQI: 修正函数取值范围
def get_input_range(func_index):
    # if func_index in [7, 14, 18, 19, 27]:
    #     return [[0, 20], [0, 20]]
    # elif func_index in [15, 17]:    # log, log10函数输入需大于0。
    #     return [[0, 20]]
    # elif func_index in [2,4]:   # arccos, arcsin定义域在[-1,1]之间
    #     return [[-1, 1]]
    # elif func_index in [3]:     # arccosh定义域在[1,+∞]。
    #     return [[1, 20]]
    # elif func_index in [16]:    # log(1+x)的定义域在[-1,+∞]。
    #     return [[-1, 20]]
    # else:
    #     return [[-10, 10]]
    # /////////////////////
    # if func_index in [26, 27, 29]:
    #     return [[-10, 10], [-10, 10]]
    # elif func_index in [31]:
    #     return [[-10, 10]]
    # elif func_index in [32]:
    #     return [[-10, 10],[-10, 10],[-10, 10]]
    # elif func_index in[28, 30]:
    #     return [[-10, 10]]
    # elif func_index in [33, 34]:
    #     return [[0, 20]]
    # ////////////////////////
    if func_index in [26, 27, 29]:
        return [[-10, 10], [-10, 10]]
    elif func_index in [31, 50, 51, 53, 60, 61, 62, 63]:
        return [[-10, 10]]
    elif func_index in [32]:
        return [[-10, 10], [-10, 10], [-10, 10]]
    elif func_index in [22,23,24,25,28, 30, 33, 34, 39, 48]:
        return [[-10, 10]]
    elif func_index in [35, 36, 37, 38, 40, 41, 42, 43, 44, 45, 46, 47]:
        return [[0, 20]]
    elif func_index in [49, 55, 56, 57, 58, 64, 65]:
        return [[0, 20]]
    elif func_index in [21,52, 54, 59]:
        return [[1, 20]]
    # 识别函数的函数定义域范围
    elif func_index in [1]:
        config = load_config()
        ranges = set_frontend_input_range()
        if ranges is None:
            default_range = [-20, 20]
            return [default_range.copy() for _ in range(input_param_count)]
        return [r.copy() for r in ranges]


# datatype for each element of the input:
def get_input_datatype(func_index):
    if func_index in [26, 27, 29]:
        return [int, int]
    elif func_index in [31, 49, 50, 51, 52, 53, 54, 55, 56,
                        57, 58, 59, 60, 61, 62, 63, 64, 65, ]:
        return [int]
    elif func_index in [32]:
        return [int, int, int]
    elif func_index in [21,22,23,24,25,28, 30, 33, 34, 35, 36, 37, 38, 39,
                        40, 41, 42, 43, 44, 45, 46, 47, 48]:
        return [float]
    elif func_index in [1]:
        config = load_config()
        datatypes = set_frontend_input_datatype()
        if datatypes is None:
            return [int for _ in range(input_param_count)]
        return datatypes.copy()
    # /////////
    # if func_index in [26, 27, 29]:
    #     return [int, int]
    # elif func_index in [31]:
    #     return [int]
    # elif func_index in [32]:
    #     return [int,int,int]
    # elif func_index in[28, 30, 33, 34]:
    #     return [float]


    # if func_index in [7, 14, 18, 19, 27]:
    #     return [float, float]
    # else:
    #     return [float]

def process_frontend_inputs():
    try:
        # 1. 验证并保存文件名
        if not set_filename_from_frontend():
            raise ValueError("文件名设置失败")

        # 2. 更新函数映射
        update_index_1_with_filename()

        # 3. 保存参数个数
        if not set_input_param_count_from_frontend():
            raise ValueError("参数个数设置失败")

        # 4. 保存输入范围
        set_frontend_input_range()

        # 5. 保存数据类型
        set_frontend_input_datatype()

        return {"status": "success", "message": "所有配置已保存"}

    except Exception as e:
        return {"status": "error", "message": str(e)}

# path to store results
output_path = "./output/np25"

# search parameters
pso_runs = 3
pso_iterations = 350




# set type and search range for coeff_range, const_range
coeff_type = int
const_type = float
coeff_range = [-2, 2]
const_range = [-10, 10]


def auto_update_on_import(func):
    """模块导入时自动执行的装饰器"""
    import functools

    @functools.wraps(func)
    def wrapper(*args, ** kwargs):
        # 在函数执行前先更新映射
        update_index_1_with_filename()
        return func(*args,** kwargs)

        # 添加模块属性，标记已装饰
        wrapper._auto_updated = True
        return wrapper

    # 装饰所有需要自动更新的函数


    # 确保模块导入时也执行一次
    update_index_1_with_filename()