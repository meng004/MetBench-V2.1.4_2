import numpy as np
import get_function
import os
import json
from numba import njit
from functools import lru_cache
import importlib.util
import types
import sys

# 将相对路径转换为绝对路径
def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)

# --------------------------
# 保持原有全局变量和配置完全不变
# --------------------------
input_program_filename = None
input_param_count = 1
frontend_input_ranges = None
frontend_input_datatypes = None
CONFIG_FILE = get_absolute_path("config.json")

map_index_func = {1: 'x', 21: 'log', 22: 'ceil', 23: 'tan', 24: 'cos', 25: 'sin', 26: 'add', 27: 'arc_length',
                  28: 'ceil', 29: 'combinations',
                  30: 'floor', 31: 'integer_square_root', 32: 'sum_of_series',
                  33: 'cos', 34: 'radians', 35: 'vol_cube', 36: 'vol_sphere',
                  37: 'vol_hemisphere', 38: 'vol_icosahedron', 39: 'abs_val',
                  40: 'surface_area_hemisphere', 41: 'surface_area_cube', 42: 'surface_area_sphere',
                  43: 'area_square', 44: 'area_circle', 45: 'dodecahedron_surface_area',
                  46: 'dodecahedron_volume', 47: 'gamma_recursive', 48: 'signum',
                  49: 'sylvester', 50: 'remove_digit', 51: 'decimal_to_negative_base_2',
                  52: 'number_of_divisors', 53: 'sum_of_digits', 54: 'sum_of_divisors',
                  55: 'double_factorial_recursive', 56: 'double_factorial_iterative', 57: 'factorial',
                  58: 'factorial_recursive', 59: 'exact_prime_factor_count', 60: 'dynamic_lucas_number',
                  61: 'num_digits', 62: 'num_digits_fast', 63: 'num_digits_faster',
                  64: 'multiplicative_persistence', 65: 'additive_persistence'}

# --------------------------
# Numba加速的函数加载系统
# --------------------------

# 函数缓存字典
_function_cache = {}
_original_function_cache = {}  # 保留原始函数用于无法编译的情况


# def load_and_compile_function(module_path):
#     """加载并尝试用Numba编译函数，保持原始函数作为后备"""
#     if module_path not in _function_cache:
#         # 原始加载方式
#         original_func = get_function.generate_function(module_path)
#         _original_function_cache[module_path] = original_func
#
#         try:
#             # 尝试获取函数源代码
#             with open(module_path, 'r') as f:
#                 source_code = f.read()
#
#             # 创建独立模块
#             module = types.ModuleType("numba_module")
#             exec(source_code, module.__dict__)
#
#             # 获取函数对象 (假设函数名为'main'或'function')
#             func = getattr(module, 'main', None) or getattr(module, 'function', None)
#
#             if func:
#                 # 使用Numba编译
#                 compiled_func = njit(func)
#                 _function_cache[module_path] = compiled_func
#             else:
#                 raise ValueError("未找到可编译的函数")
#
#         except Exception as e:
#             print(f"警告: 无法用Numba编译 {module_path}, 使用原始函数: {str(e)}")
#             _function_cache[module_path] = original_func
#
#     return _function_cache[module_path]


# --------------------------
# 优化后的program函数 (接口完全不变)
# --------------------------

def load_and_compile_function(module_path):
    """增强版的函数加载和编译，解决各类Numba兼容性问题"""
    abs_module_path = get_absolute_path(module_path)
    # if module_path not in _function_cache:
    if abs_module_path  not in _function_cache:
        # 原始加载方式
        original_func = get_function.generate_function(abs_module_path)
        _original_function_cache[abs_module_path] = original_func
        try:
            # 尝试多种编码读取文件
            with open(abs_module_path, 'r', encoding='utf-8') as f:
                source_code = f.read()
            # 创建独立模块
            module = types.ModuleType("numba_module")
            exec(source_code, module.__dict__)
            # 获取main函数
            if not hasattr(module, 'main'):
                raise ValueError("模块缺少main函数")
            func = module.main
            # 根据函数类型进行特殊处理
            func_name = os.path.basename(abs_module_path)[:-3]
            if func_name in ['gamma_recursive', 'factorial_recursive','double_factorial_recursive', 'sylvester']:
                # 递归函数特殊处理
                _function_cache[abs_module_path] = original_func
                return original_func
            elif func_name in ['remove_digit']:
                # 生成器函数特殊处理
                _function_cache[abs_module_path] = original_func
                return original_func
            # 尝试编译
            compiled_func = njit(func)
            # 根据函数参数数量准备测试输入
            arg_count = func.__code__.co_argcount
            if arg_count == 1:
                test_input = 1.0
            else:
                test_input = tuple(1.0 for _ in range(arg_count))
            # 类型安全测试执行
            try:
                if isinstance(test_input, tuple):
                    compiled_func(*test_input)
                else:
                    compiled_func(test_input)
                _function_cache[abs_module_path] = compiled_func
            except Exception as e:
                _function_cache[abs_module_path] = original_func
        except Exception as e:
            _function_cache[abs_module_path] = original_func
    return _function_cache[abs_module_path]

def program(i, func_index):
    """接口完全保持不变，内部使用Numba加速"""
    # 内置函数处理 (保持不变)
    if func_index == 21:
        return np.log(i)
    elif func_index == 22:
        return np.ceil(i)
    elif func_index == 23:
        return np.tan(i)
    elif func_index == 24:
        return np.cos(i)
    elif func_index == 25:
        return np.sin(i)

    # 外部函数处理
    module_path = get_module_path(func_index)
    if not module_path:
        raise ValueError(f"无效的函数索引: {func_index}")

    # 加载/编译函数
    func = load_and_compile_function(module_path)

    # 准备参数 (保持原有逻辑完全不变)
    value = []
    if hasattr(i, '__len__'):
        if len(i) == 1:
            value.append(i[0])
        else:
            value.extend(i)
    else:
        value.append(i)

    # 执行函数 (自动处理编译和未编译两种情况)
    if func in _original_function_cache.values():
        # 使用原始方式执行
        return get_function.calculate_value_from_function(func, value)
    else:
        # 使用编译后的函数执行
        return func(*value)


def get_module_path(func_index):
    """根据函数索引获取模块路径 (保持原有逻辑)"""
    if func_index == 1:
        config = load_config()
        filename = config["input_program_filename"]
        # return f"function/{filename}" if filename else None
        return get_absolute_path(f"function/{filename}") if filename else None
    else:
        func_name = map_index_func.get(func_index)
        # return f"function/{func_name}.py" if func_name else None
        return get_absolute_path(f"function/{func_name}.py") if func_name else None


# --------------------------
# 保持所有原有辅助函数完全不变
# --------------------------

def load_config():
    """加载配置文件 (完全不变)"""
    if not os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, 'w') as f:
            json.dump({
                "input_program_filename": None,
                "input_param_count": 1,
                "frontend_input_ranges": None,
                "frontend_input_datatypes": None,
                "repet":1
            }, f)
    with open(CONFIG_FILE, 'r') as f:
        return json.load(f)


def save_config(config):
    """保存配置文件 (完全不变)"""
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=4)

def set_filename_from_frontend():
    """(完全不变)"""
    config = load_config()
    filename = config["input_program_filename"]
    if not filename or not isinstance(filename, str) or not filename.strip():
        # print("错误：文件名无效！")
        return None

    current_dir = os.path.dirname(os.path.abspath(__file__))
    function_dir = os.path.join(current_dir, "function")
    os.makedirs(function_dir, exist_ok=True)
    target_path = os.path.join(function_dir, filename)
    if not os.path.exists(target_path):
        try:
            with open(target_path, 'w') as f:
                f.write("")
        except Exception as e:
            # print(f"创建文件失败: {e}")
            return None
    return filename


def get_filename_without_extension():
    """(完全不变)"""
    config = load_config()
    filename = config["input_program_filename"]
    if filename and isinstance(filename, str):
        return filename[:-3] if filename.endswith('.py') else filename
    return None


def set_input_param_count_from_frontend():
    """(完全不变)"""
    config = load_config()
    param_count = config["input_param_count"]
    if not isinstance(param_count, int) or param_count < 0:
        # print("错误：参数个数必须是正整数！")
        return None
    return param_count

def set_frontend_input_range():
    """(完全不变)"""
    config = load_config()
    range_str = config["frontend_input_ranges"]
    param_count = config["input_param_count"]
    if not range_str:
        # print("错误：输入范围为空！")
        return None
    stripped = range_str.replace(" ", "")

    try:
        if param_count == 1:
            if not (stripped.startswith('(') and stripped.endswith(')')):
                raise ValueError("单参数范围格式应为'(min,max)'")
            min_val, max_val = map(int, stripped[1:-1].split(','))
            range_str = [[min_val, max_val]]
        else:
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

    for r in range_str:
        if r[0] >= r[1]:
            raise ValueError(f"范围最小值{r[0]}不能大于等于最大值{r[1]}")
    return range_str


def set_frontend_input_datatype():
    """(完全不变)"""
    config = load_config()
    datatype_str = config["frontend_input_datatypes"]
    param_count = config["input_param_count"]
    stripped = datatype_str.replace(" ", "")

    try:
        if param_count == 1:
            if stripped not in ["int", "float"]:
                raise ValueError("单参数数据类型应为 'int' 或 'float'")
            datatype_str = [eval(stripped)]
        else:
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
    except (ValueError, SyntaxError) as e:
        raise ValueError(f"无效的数据类型格式: {str(e)}") from e
    return datatype_str


def get_function_map():
    """(完全不变)"""
    update_index_1_with_filename()
    return map_index_func.copy()


def update_index_1_with_filename():
    """(完全不变)"""
    global map_index_func
    filename = get_filename_without_extension()
    if filename is not None:
        map_index_func[1] = filename
    else:
        print("No filename available, keeping original value 'x'")
    return map_index_func


def getNEI(func_index):
    """(完全不变)"""
    if func_index in [26, 27, 29]:
        return 2
    elif func_index in [32]:
        return 3
    elif func_index in [21, 22, 23, 24, 25, 28, 30, 31, 33, 34, 35, 36, 37, 38, 39,
                        40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
                        50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
                        60, 61, 62, 63, 64, 65]:
        return 1
    elif func_index in [1]:
        config = load_config()
        input_param_count = set_input_param_count_from_frontend()
        if input_param_count is not None:
            return input_param_count
        return 1


def getNEO(func_index):
    """(完全不变)"""
    return 1


def get_input_range(func_index):
    """(完全不变)"""
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
        config = load_config()
        ranges = set_frontend_input_range()
        if ranges is None:
            default_range = [-20, 20]
            return [default_range.copy() for _ in range(input_param_count)]
        return [r.copy() for r in ranges]


def get_input_datatype(func_index):
    """(完全不变)"""
    if func_index in [26, 27, 29]:
        return [int, int]
    elif func_index in [31, 49, 50, 51, 52, 53, 54, 55, 56,
                        57, 58, 59, 60, 61, 62, 63, 64, 65, ]:
        return [int]
    elif func_index in [32]:
        return [int, int, int]
    elif func_index in [21, 22, 23, 24, 25, 28, 30, 33, 34, 35, 36, 37, 38, 39,
                        40, 41, 42, 43, 44, 45, 46, 47, 48]:
        return [float]
    elif func_index in [1]:
        config = load_config()
        datatypes = set_frontend_input_datatype()
        if datatypes is None:
            return [int for _ in range(input_param_count)]
        return datatypes.copy()


def process_frontend_inputs():
    """(完全不变)"""
    try:
        if not set_filename_from_frontend():
            raise ValueError("文件名设置失败")
        update_index_1_with_filename()
        if not set_input_param_count_from_frontend():
            raise ValueError("参数个数设置失败")
        set_frontend_input_range()
        set_frontend_input_datatype()
        return {"status": "success", "message": "所有配置已保存"}
    except Exception as e:
        return {"status": "error", "message": str(e)}


# --------------------------
# 初始化函数预加载
# --------------------------

def init_functions():
    """预加载所有已知函数"""
    for func_index, func_name in map_index_func.items():
        if func_index != 1:  # 索引1的函数是动态的
            try:
                module_path = f"function/{func_name}.py"
                abs_module_path = get_absolute_path(module_path)
                # if os.path.exists(module_path):
                if os.path.exists(abs_module_path):
                    load_and_compile_function(module_path)
            except Exception as e:
                print(f"函数预加载警告: {func_name} - {str(e)}")


# 在模块导入时预加载
init_functions()

# --------------------------
# 保持原有配置不变
# --------------------------
# output_path = "./output/np25"
output_path = get_absolute_path("./output/np25")
pso_runs = 3
pso_iterations = 350
coeff_type = int
const_type = float
coeff_range = [-2, 2]
const_range = [-10, 10]
func_indices = list(range(1, 65))
parameters_collection = ["2_1_1_1_1"]