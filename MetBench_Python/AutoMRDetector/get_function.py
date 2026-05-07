import importlib.util
import inspect
import os
from typing import Dict, Any
import numpy as np


# 从本地获取待测函数文件并生成相应模块 参数含义：模块名称name，文件路径path
# def generate_module(name, path):
#     # 基于给定的名称和文件路径创建一个模块规范对象
#     module_spec = importlib.util.spec_from_file_location(name, path)
#
#     # 基于给定的模块规范创建一个模块对象
#     function_module = importlib.util.module_from_spec(module_spec)
#
#     # 执行模块代码，并将其加载到模块对象中
#     module_spec.loader.exec_module(function_module)
#
#     # 返回生成的模块对象
#     return function_module
#
# # 获取被测函数
# def generate_function(module_path):
#     # 根据文件路径获取文件名
#     file_name = os.path.basename(module_path)
#     # 获取模块名
#     module_name = os.path.splitext(file_name)[0]
#     Function = generate_module(module_name, module_path)
#
#     # 获取模块中的所有函数
#     functions = inspect.getmembers(Function, inspect.isfunction)
#
#     # 模块只有一个函数，故选择第一个函数进行调用
#     function_name, function = functions[0]
#
#     return function
#
# # 根据待测函数计算y值  参数含义：根据维度总和的变量x的列表group，待测函数function，存储计算结果的列表result_array、数据字典y_results，函数输出维度MR_output_dimension
# def calculate_value_from_function(function,value):
#         function_values = function(*value)  # 调用函数 function，并传递数值数组作为参数，计算函数值
#         return function_values

# 内部缓存
_module_cache: Dict[str, Any] = {}
_function_cache: Dict[str, Any] = {}

def generate_module(name, path):
    """保持原函数签名不变的缓存版本"""
    cache_key = f"{name}_{path}"
    if cache_key not in _module_cache:
        module_spec = importlib.util.spec_from_file_location(name, path)
        function_module = importlib.util.module_from_spec(module_spec)
        module_spec.loader.exec_module(function_module)
        _module_cache[cache_key] = function_module
    return _module_cache[cache_key]

def generate_function(module_path):
    """保持原函数签名不变的缓存版本"""
    if module_path not in _function_cache:
        file_name = os.path.basename(module_path)
        module_name = os.path.splitext(file_name)[0]
        Function = generate_module(module_name, module_path)
        functions = inspect.getmembers(Function, inspect.isfunction)
        _function_cache[module_path] = functions[0][1]
    return _function_cache[module_path]

def calculate_value_from_function(function, value):
    """原函数保持不变，因为这是核心计算逻辑"""
    return function(*value)

# 测试例子
# module_path=r"D:\1_Project\C#_Project_100\MetBench_101\MetBench\未开发的新功能\AutoMR-MT-Many-master-cos-100\AutoMR-MT-Many-master-cos-100\AutoMR-MT-Many-master\function\sin.py"
# function=generate_function(module_path)
# value = [0]
# result = calculate_value_from_function(function, value)
# o = result


# value=[10,9]
# # v2=np.sin(10)
# result=calculate_value_from_function(function,value)
# print(result)
# # print(v2)

