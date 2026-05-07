import importlib.util
import inspect
import os
import random
import re
import numpy as np
from matplotlib import pyplot as plt
from math import pi,sqrt
from datetime import datetime  # 获取当前时间戳
from twodimJavaInterpreter import *
from typing import List, Union
import csv
import  pandas as pd
import time
# 去除花括号
def remove_braces(text):
    return re.sub(r'[{}]', '', text)

# 创建文件夹，并返回该文件夹的路径。 参数含义：定义的文件夹名称folder_name
def create_folder(folder_name):
    # 获取本机用户名
    username = os.getenv('USERNAME')
    # 构建临时目录路径
    temp_dir = os.path.join('C:\\Users', username, 'AppData', 'Local', 'Temp')

    # 将临时文件夹路径和folder_name拼接起来，得到要创建的文件夹的完整路径
    folder_path = os.path.join(temp_dir, folder_name)
    # 判断文件姐是否已经存在，若不存在则新建文件夹
    if not os.path.exists(folder_path):
        os.makedirs(folder_path)
    return folder_path

# 生成随机数据 参数含义：数据维度MR_dimension、随机生成数据的次数random_data_count、数据最小值random_data_min_value、数据最大值random_data_max_value
# 生成随机数据 参数含义：随机生成数据的次数random_data_count、数据最小值random_data_min_value、数据最大值random_data_max_value、待测函数参数类型
# def generate_random_data(
#     random_data_count: int,
#     random_data_min_value: Union[int, float],
#     random_data_max_value: Union[int, float],
#     param_type: List[type]
# ) -> List[List[Union[int, float]]]:
#     return [
#         [
#             random.randint(random_data_min_value, random_data_max_value) if t == int else
#             random.uniform(random_data_min_value, random_data_max_value)
#             for t in param_type
#         ]
#         for _ in range(random_data_count)
#     ]
def generate_random_data(
        random_data_count: int,
        random_data_min_value: Union[int, float],
        random_data_max_value: Union[int, float],
        param_type: List[type]
) -> List[List[Union[int, float]]]:
    # 生成32位范围内的随机种子
    seed = int(time.time() * 1000) % (2 ** 32)
    random.seed(seed)
    np.random.seed(seed)

    return [
        [
            # 整数：使用numpy的randint 离散均匀分布
            int(np.random.randint(random_data_min_value, random_data_max_value + 1))
            if t == int else

            # 浮点数：使用numpy的uniform 连续均匀分布
            float(np.random.uniform(random_data_min_value, random_data_max_value))

            for t in param_type
        ]
        for _ in range(random_data_count)
    ]

# 从指定的文件路径中获取待测函数的代码，可以动态地加载指定路径的.py文件，并创建一个可以直接调用这些函数的模块对象。生成的模块对象可以在测试中使用，以确保函数的正确性。
# 从本地获取待测函数文件并生成相应模块 参数含义：模块名称name，文件路径path
def generate_module(name, path):
    # 基于给定的名称和文件路径创建一个模块规范对象
    module_spec = importlib.util.spec_from_file_location(name, path)

    # 基于给定的模块规范创建一个模块对象
    function_module = importlib.util.module_from_spec(module_spec)

    # 执行模块代码，并将其加载到模块对象中
    module_spec.loader.exec_module(function_module)

    # 返回生成的模块对象
    return function_module

# 根据指定的文件路径，动态生成模块对象并获取其中第一个函数的参数信息，返回函数名、参数类型等信息
def generate_function(module_path):
    # 根据文件路径获取文件名
    file_name = os.path.basename(module_path)
    # 获取模块名
    module_name = os.path.splitext(file_name)[0]
    Function = generate_module(module_name, module_path)

    # 获取模块中的所有函数
    functions = inspect.getmembers(Function, inspect.isfunction)

    # 模块只有一个函数，故选择第一个函数进行调用
    function_name, function = functions[0]

    # 获取函数的参数信息
    parameters = inspect.signature(function).parameters

    # 遍历参数信息，获取参数类型
    param_types = []
    for param_name, param in parameters.items():
        param_type = param.annotation
        if param_type is inspect._empty:
            param_type = float  # 如果参数没有注释类型，则将其设置为 float
        param_types.append(param_type)

    return module_name, function, param_types

# 生成x的变量字典，变量名及其对应值 参数含义：随机生成的一行数据row
def generate_variable_assignments_for_x(row,MR_Input_dimension):
    variable_names = [f"x_{group + 1}{index + 1}" for group in range(MR_Input_dimension) for index in range(len(row))]
    variable_assignments = dict(zip(variable_names, row))
    return variable_assignments

# 分割表达式并返回表达式列表 参数含义：输入关系r_list
def extract_r(r_list):
    expressions = [] # 空列表，用于存储分割后的表达式
    current_expression = "" # 空字符串，用于存储当前正在构建的表达式
    parentheses_count = 0 # 表示当前表达式中的括号数量

    for char in r_list:
        # 将表达式中的每个字符chaar追加到current_expression中
        current_expression += char

        if char == "(":
            parentheses_count += 1
        elif char == ")":
            parentheses_count -= 1

        # 若parentheses_count为0且char为,，表示遇到了分割点
        if parentheses_count == 0 and char == ",":
            # 将current_expression去除首尾空格后添加到expressions列表中
            expressions.append(current_expression.strip())
            current_expression = "" # 重置为空字符串。

    expressions.append(current_expression)
    return expressions

# 更新字典 参数含义：输入关系中的表达式expression_of_r，根据表达式计算的变量x的值calculate_x，变量x的数据字典variable_assignments
def update_variable_assignments(expression_of_r, calculate_x, variable_assignments):
    # 提取出变量名
    variables = re.search(r'Eq\(([^,]+),', expression_of_r)

    if variables:
        variable_to_update = variables.group(1).strip()  # 将提取到的变量名去除首尾空格
        variable_assignments[variable_to_update] = calculate_x  # 并将根据输入关系计算出的变量x的值赋给 variable_assignments 字典中对应的变量名

# 根据输入模式r计算x的值  参数含义：输入关系r，变量x的数据字典variable_assignments
def calculate_x_from_r(r, variable_assignments):
    x_result = []  # 存储计算结果的列表
    expressions_of_r = extract_r(r)  # 提取 r 中的表达式

    for expr in expressions_of_r:
        variables = re.findall(r'x_\d+', expr)  # 从表达式中提取变量名

        calculate_x = calculate_value_from_expression(expr, variable_assignments)  # 根据表达式计算 x 的值
        x_cal = f"{variables[0]}:", calculate_x  # 创建变量和值的元组，形如：'x_1:', 3.14
        x_result.append(x_cal)  # 将变量和值的元组添加到结果列表中

        update_variable_assignments(expr, calculate_x, variable_assignments)  # 更新变量赋值字典，将计算得到的 x 值赋给相应的变量

    return x_result, variable_assignments  # 返回结果列表和更新后的变量赋值字典




# 把数据字典转换成列表  参数含义：变量x的数据字典dic
def transform_dict_to_list(dic):
    result = []  # 存储结果的列表
    temp_dict = {}  # 临时字典

    # 分组
    for key, value in dic.items():
        digit = int(re.search(r'\d+', key).group()[0])  # 从键中提取数字
        if digit not in temp_dict:
            temp_dict[digit] = []  # 创建一个空列表来存储每个数字对应的值
        temp_dict[digit].append(value)  # 将值添加到对应数字的列表中

    # 构建字符串并添加到结果列表
    for digit, values in temp_dict.items():
        values_str = ", ".join(str(val) for val in values)  # 将每个数字对应的值转换为字符串，并用逗号分隔
        result.append(f'x_{digit}=({values_str})')  # 构建形如 'x_1=(value1, value2, ...)' 的字符串，并添加到结果列表中

    return result

def calculate_value_from_function_python(group, function, result_array, y_results, MR_output_dimension, types: List[type]):
    for i, data in enumerate(group, start=1):
        variable = re.findall(r"x_(\d+)", data)  # 从字符串中提取变量名（形如 x_1、x_2）
        if variable:
            values = re.findall(r"\((.*?)\)", data)  # 从字符串中提取括号内的数值（形如 -1.23、4.56）
            if values:
                values = [value.replace('pi', str(pi)) for value in values]  # 将 'pi' 替换为对应的数值
                values = [convert_type(value, types[index]) for index, value in enumerate(','.join(values).split(','))]  # 根据 types 参数转换数值类型
                values = np.array(values)

                function_values = function(*values)  # 调用函数 function，并传递数值数组作为参数，计算函数值

                for j in range(1, MR_output_dimension + 1):
                    y_variable = f"y_{i}{j}"  # 根据索引 i 和 j 生成输出变量名（形如 y_11、y_12、y_21、y_22）
                    y_results[y_variable] = function_values  # 将函数值存储到结果字典 y_results 中
                    result_array.append({y_variable: function_values})  # 将变量名和函数值作为字典添加到结果列表 result_array 中

    return result_array, y_results

def convert_type(value, target_type):
    if target_type == int:
        return int(value)
    elif target_type == float:
        return float(value)
    else:
        return value

# # JAVA接入点
def calculate_value_from_function_java(group, function_name, result_array, y_results,MR_Input_dimension, MR_output_dimension):
    for i, data in enumerate(group, start=1):
        # print(data)
        variable = re.findall(r"x_(\d+)", data)  # 从字符串中提取变量名（形如 x_1、x_2）
        if variable:
            values = re.findall(r"\((.*?)\)", data)  # 从字符串中提取数值（形如 -1.23、4.56）
            if values:
                # print(values)
                values = [value.replace('pi', str(pi)) for value in values]  # 将 'pi' 替换为对应的数值
                values = [float(value) for value in ','.join(values).split(',')]  # 拆分逗号分隔的字符串，并转换为浮点数
                values = np.array(values)
                # 调用Run_Java_Code模块的函数，传入Java模块的名称module_name和变量的值，通过Java环境计算得到y值
                # 多维
                # 获取本机用户名
                # function = "AddTwoNumbers"
                # print("values",values)
                function_values = Run_Java_Code(function_name, MR_Input_dimension, *values)
                # print(function_values)

                for j in range(1, MR_output_dimension + 1):
                    y_variable = f"y_{i}{j}"  # 根据索引 i 和 j 生成输出变量名（形如 y_11、y_12、y_21、y_22）
                    y_results[y_variable] = function_values  # 将函数值存储到结果字典 y_results 中
                    result_array.append({y_variable: function_values})  # 将变量名和函数值作为字典添加到结果列表 result_array 中

    return result_array, y_results

# 根据输入输出关系进行计算  参数含义：关系表达式expression，变量的数据字典variable_assignments
def calculate_value_from_expression(expression, variable_assignments):
    expression = re.search(r'Eq\([^,]+,\s*(.+)\)', expression)
    if expression:
        expression_to_calculate = expression.group(1)
        # 在expression中查找所有 x_1、x_2等形式的变量，并将它们存储在variables列表中。
        variables = re.findall(r'[xy]_\d+', expression_to_calculate)
        # 遍历variables中的每个变量
        for variable in variables:
            # 如果变量存在于variable_assignments字典中，就使用对应的变量值替换expression中的变量。
            if variable in variable_assignments:
                expression_to_calculate = expression_to_calculate.replace(variable, str(variable_assignments[variable]))

        # 对更新后的 expression 进行求值，返回得到最终的计算结果。
        return eval(expression_to_calculate)

# 判断蜕变关系测试通过与否  参数含义：真实值actual，变量y的数据字典y_result，变量名variable_to_cal，阈值threshold，通过数量true_count，不通过数量false_count
def determine_pass_ornot(actual, y_result, variable_to_cal, threshold, true_count, false_count):
    diff = abs(actual - y_result[variable_to_cal[0]])  # 计算 实际值 和 期望值 的差值的绝对值
    if diff <= threshold:  # 如果差值小于等于阈值
        true_count += 1  # 将 通过数量true_count 加 1
    else:
        false_count += 1  # 否则将 不通过数量false_count 加 1

    return true_count, false_count  # 返回更新后的 true_count 和 false_count

# 对计算结果进行对应排序  参数含义：根据待测函数计算的y值Y1, Y2
def sort_and_zip(Y1, Y2,actual):
    # 将Y1和Y2进行打包，得到一个元组列表combined
    combined = list(zip(Y1, Y2,actual))
    # 按照元组的第一个元素（即Y1）的值进行升序排序。
    combined.sort(key=lambda x: x[0])
    # 对排序后的combined进行解压缩，得到排序后的Y1_sorted和Y2_sorted。
    Y1_sorted, Y2_sorted,actual = zip(*combined)

    return Y1_sorted, Y2_sorted,actual

# 测试结果可视化  参数含义：被测函数模块名module_name，已排序的计算结果Y1_sorted、Y2_sorted，实际值actual，输出关系R
def plot_graph(module_name, Y1_sorted, Y2_sorted, actual, R):
    plt.figure(module_name)  # 创建一个新的图形窗口，并指定图形的名称

    # plt.plot(Y1_sorted, Y2_sorted, 'o-', label='(Y1, Y2)')  # 绘制以 Y1_sorted 为 x 坐标，Y2_sorted 为 y 坐标的散点图线
    plt.scatter(Y1_sorted, Y2_sorted, marker='x', color='red', s=100, label='(Y1, Y2)')

    plt.xlabel('Y1')  # 设置 x 轴标签为 'Y1'
    plt.ylabel('Y2')  # 设置 y 轴标签为 'Y2'

    # 绘制第二条图线：根据输出关系 R 得出的值
    # plt.plot(Y1_sorted, actual, label=f'R: {R}')  # 绘制以 Y1_sorted 为 x 坐标，actual 为 y 坐标的折线图线，并在图例中显示 R 的值
    plt.scatter(Y1_sorted, actual, label=f'R: {R}')
    plt.legend()  # 显示图例

# 保存绘图 参数含义：模块名module_name，文件夹folder
def save_plot(module_name, folder):
    # 获取当前的具体时间
    current_time = datetime.now()
    # 格式化时间戳
    timestamp = current_time.strftime("%Y%m%d%H%M%S%f")
    # 拼接文件名和时间戳
    image_name = module_name + "_" + timestamp + ".png"
    # 拼接保存路径
    save_path = os.path.join(folder, image_name)
    plt.savefig(save_path)
    # 输出图片路径
    print(save_path)

# 计算 参数含义：模块路径module_path，输入关系r，输出关系R，随机次数random_count，待测函数输入维度MR_Input_dimension，待测函数输出维度MR_output_dimension，
# 生成的随机值的最小值random_data_min_value，最大值random_data_max_value
def calculate(module_path, r, R, random_count, MR_Input_dimension, MR_output_dimension, random_data_min_value,
              random_data_max_value,threshold):
    true_count = 0
    false_count = 0
    # filename = "../PythonandJava_1214/sum_of_digits.py"

    filename=module_path

    if filename.endswith(".py"):
        # print("以.py结尾")
        # Python验证：
        module_name, function, parameter_types_eval = generate_function(module_path)  # 从指定模块路径生成模块名称和函数对象
    elif filename.endswith(".java"):
        # print("以.java结尾")
         # Java获取参数类型
        # java_file_path="AddTwoNumbers.java"
        method_name,x_count,param_types,parameter_types_eval=get_java_method_name(module_path)
        # print(parameter_types)


    STC = [] # 在循环外声明一个空列表，用于存储所有转换后的结果
    FTC = []
    result_array = []  # 存储计算结果的列表
    y_results = {}  # 存储中间计算结果的字典
    actual = []  # 存储根据 R 计算得出的实际值的列表

    random_x = generate_random_data(random_count, random_data_min_value,
                                    random_data_max_value,parameter_types_eval)  # 生成随机数据


    for i, row in enumerate(random_x, 1):
        variable_assignments = generate_variable_assignments_for_x(row, MR_Input_dimension)  # 为随机数据生成变量赋值
        stc = generate_variable_assignments_for_x(row, MR_Input_dimension)
        # print("original_x:", variable_assignments)
        STC.append(stc)
        # print(STC)
        x_result, variable_assignments = calculate_x_from_r(r, variable_assignments)  # 根据 r 计算 x 值
        # print("x_result:", x_result)
        FTC.append(x_result)

        group = transform_dict_to_list(variable_assignments)  # 将变量赋值转换为列表形式

        # print("Group x values:", group)


        front_path, file_extension = os.path.splitext(module_path)
        # 提取文件名
        filename = os.path.basename(front_path)
        # 去掉文件扩展名
        module_name = os.path.splitext(filename)[0]
        # 若为Python语言
        if (file_extension == '.py'):
            module_name, function, param_types = generate_function(module_path)  # 从指定模块路径生成模块名称和函数对象
            # 从指定模块路径生成模块名称和函数对象
            # python计算
            result_array, y_results = calculate_value_from_function_python(group, function, result_array, y_results,
                                                                           MR_output_dimension,param_types)  # 根据函数计算 y 值
        # 若为Java语言
        elif (file_extension == '.java'):
            #java计算
            result_array, y_results = calculate_value_from_function_java(group, filename, result_array, y_results,MR_Input_dimension,
                                                                    MR_output_dimension)  # 根据函数计算 y 值
        # print("output_y", y_results)

        variable_to_cal = re.findall(r'[xy]_\d+', R)  # 从表达式 R 中提取要计算的变量

        y_from_R = calculate_value_from_expression(R, y_results)  # 根据表达式 R 计算得出的 y 值
        # print("y_expected=", y_results[variable_to_cal[0]], "; y_actual=", y_from_R)
        actual.append(y_from_R)

        true_count, false_count = determine_pass_ornot(y_from_R, y_results, variable_to_cal, threshold, true_count,
                                                       false_count)  # 判断计算结果是否符合条件

        # print("\n")
        # print(STC)
    return result_array, actual, module_name, true_count, false_count,STC,FTC


def save_test_results( actual, STC, FTC, Y1, Y2,threshold):
    try:
        # 获取当前工作目录的绝对路径
        current_dir = os.path.dirname(os.path.abspath(__file__))

        # 创建MTResult文件夹（如果不存在）
        mtresult_dir = os.path.join(current_dir, "MTResult")
        os.makedirs(mtresult_dir, exist_ok=True)

        stc_data = []
        for stc in STC:
            if isinstance(stc, dict):
                # 如果是字典，只提取值并按x_11, x_12,...的顺序排序
                values = [stc[key] for key in sorted(stc.keys())]
                stc_data.append(values)
            elif isinstance(stc, (list, tuple)):
                # 如果是列表/元组，直接使用（假设已经是纯数值）
                stc_data.append(list(stc))
            else:
                # 单个值转换为单元素列表
                stc_data.append([stc])
        # 确定STC列数
        max_stc_cols = max(len(item) for item in stc_data) if stc_data else 1
        stc_columns = [f"STC_{i + 1}" for i in range(max_stc_cols)]

        stc_df = pd.DataFrame(stc_data, columns=stc_columns)
        stc_path = os.path.join(mtresult_dir, "STC.csv")
        stc_df.to_csv(stc_path, index=False)
        print(f"{stc_path}")

        # 2. 保存FTC数据到FTC.csv
        ftc_data = []
        for ftc in FTC:
            if isinstance(ftc, dict):
                # 字典类型：按key排序提取值
                values = [ftc[key] for key in sorted(ftc.keys())]
                ftc_data.append(values)
            elif isinstance(ftc, (list, tuple)) and all(isinstance(item, tuple) for item in ftc):
                values = [val for (key, val) in sorted(ftc, key=lambda x: x[0])]
                ftc_data.append(values)
            elif isinstance(ftc, (list, tuple)):
                # 普通列表/元组：直接使用数值
                ftc_data.append([x for x in ftc if isinstance(x, (int, float))])
            else:
                # 单个值：转换为单元素列表
                ftc_data.append([ftc] if isinstance(ftc, (int, float)) else [])

        # 确定FTC列数（取最大列数）
        max_ftc_cols = max(len(item) for item in ftc_data) if ftc_data else 1
        ftc_columns = [f"FTC_{i + 1}" for i in range(max_ftc_cols)]

        # 填充None确保每行列数一致
        ftc_data_filled = []
        for row in ftc_data:
            ftc_data_filled.append(row + [None] * (max_ftc_cols - len(row)))

        # 创建DataFrame并保存
        ftc_df = pd.DataFrame(ftc_data_filled, columns=ftc_columns)
        ftc_path = os.path.join(mtresult_dir, "FTC.csv")
        ftc_df.to_csv(ftc_path, index=False)
        print(f"{ftc_path}")

        tolerance = threshold  # 允许的浮点数误差范围

        # 确保所有数据长度一致
        max_len = max(len(Y1), len(Y2), len(actual))
        Y1_filled = Y1 + [None] * (max_len - len(Y1))
        Y2_filled = Y2 + [None] * (max_len - len(Y2))
        actual_filled = actual + [None] * (max_len - len(actual))

        # 计算IsPass列
        is_pass = []
        for y2, act in zip(Y2_filled, actual_filled):
            if y2 is None or act is None:
                is_pass.append(None)
            else:
                is_pass.append(1 if abs(y2 - act) < tolerance else 0)

        combined_data = {
            "Y1": Y1_filled,
            "Y2": Y2_filled,
            "Actual": actual_filled,
            "IsPass": is_pass  # 新增的通过标记列
        }

        combined_df = pd.DataFrame(combined_data)
        combined_path = os.path.join(mtresult_dir, "combined.csv")
        combined_df.to_csv(combined_path, index=False)
        print(f"{combined_path}")

        return True, "数据保存成功"
    except Exception as e:
        return False, f"保存数据时出错: {str(e)}"

# 二元多维函数的蜕变关系测试 参数含义：输入关系r，输出关系R，待测函数文件名File_name，待测函数输入维度MR_input_dimension，输出维度MR_output_dimension，
# 生成的随机值的最小值random_data_min_value，最大值random_data_max_value，随机次数random_data_count
def run_multidimensional(r, R, file_name, MR_input_dimension, MR_output_dimension, random_data_min_value, random_data_max_value, random_count,threshold):
    r=remove_braces(r)
    R=remove_braces(R)

    # 文件夹名称
    Main_folder_name = "MetBench"
    root_folder1_name = "MR_SUT"
    root_folder2_name = "MT"

    Main_folder = create_folder(Main_folder_name)  # 创建主文件夹

    MR_SUT_folder = create_folder(os.path.join(Main_folder, root_folder1_name))   # 创建MR_SUT文件夹

    module_path = os.path.join(MR_SUT_folder, file_name)  # 构建模块文件路径

    root_folder2_name = create_folder(os.path.join(Main_folder, root_folder2_name))  # 创建MT文件夹

    # 进行计算
    result_array, actual, module_name,true_count,false_count,STC,FTC = calculate(module_path, r, R, random_count,
                                                                         MR_input_dimension, MR_output_dimension, random_data_min_value, random_data_max_value,threshold)

    # 获取变量名
    variable_names = []
    # print(result_array)
    for item in result_array:
        for key in item.keys():
            variable_names.append(key)
    Y1 = []
    Y2 = []

    for item in result_array:
        if variable_names[0] in item:
            Y1.append(item[variable_names[0]])
        if variable_names[1] in item:
            Y2.append(item[variable_names[1]])

    save_test_results(actual, STC, FTC, Y1, Y2,threshold)

    Y1_sorted, Y2_sorted,actual= sort_and_zip(Y1, Y2,actual)  # 对计算结果进行排序

    true_rate = (true_count / random_count) * 100  # 计算通过率
    false_rate = (false_count / random_count) * 100  # 计算不通过率
    formatted_true_ratio = "{:.2f}%".format(true_rate)  # 进行百分比转换
    formatted_false_ratio = "{:.2f}%".format(false_rate)  # 进行百分比转换


    # 输出比率
    print(formatted_true_ratio)
    print(formatted_false_ratio)

    # 若待测函数输出维度为1，则进行可视化操作
    if (MR_output_dimension == 1):
        # actual=sorted(actual)  # 对实际值进行排序
        # print("Y1",Y1_sorted)
        # print("期望值",Y2_sorted)
        # print("真实值",actual)
        #
        # # 输出绘制点的坐标
        # for i in range(len(Y1_sorted)):
        #     print("(",Y1_sorted[i],",",Y2_sorted[i],")")
        #     print("(", Y1_sorted[i], ",", actual[i], ")")
        #     print("\n")

        # 准备CSV文件名（使用模块名和当前时间）
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        csv_filename = f"{module_name}_MR_visualization_{timestamp}.csv"

        # 获取绝对路径
        abs_root_folder2 = os.path.abspath(root_folder2_name)
        csv_path = os.path.join(abs_root_folder2, csv_filename)  # 保存到MT文件夹中

        # 确保目录存在
        os.makedirs(abs_root_folder2, exist_ok=True)

        # 写入CSV文件
        with open(csv_path, mode='w', newline='', encoding='utf-8') as csv_file:
            writer = csv.writer(csv_file)

            # 写入标题行（新增"是否通过"列）
            writer.writerow(["Y1", "Y2", "Actual", "IsPass"])

            # 写入数据行
            for i in range(len(Y1_sorted)):
                # 判断是否通过：Y1_sorted[i]与Y2_sorted[i]相同则为1，否则为0
                tolerance = threshold  # 允许的误差范围
                is_passed = 1 if abs(Y2_sorted[i] - actual[i]) < tolerance else 0
                # is_passed = 1 if Y1_sorted[i] == Y2_sorted[i] else 0

                writer.writerow([
                    Y1_sorted[i],
                    Y2_sorted[i],
                    actual[i],
                    is_passed  # 新增的是否通过列
                ])

        # print(f"可视化数据已保存到: {csv_path}")

        # # 写入CSV文件
        # with open(csv_path, mode='w', newline='', encoding='utf-8') as csv_file:
        #     writer = csv.writer(csv_file)
        #
        #     # 写入标题行
        #     writer.writerow(["输入值(X)", "期望输出(Y2)", "实际输出(Actual)", "蜕变关系(R)"])
        #
        #     # 写入数据行
        #     for i in range(len(Y1_sorted)):
        #         writer.writerow([
        #             Y1_sorted[i],
        #             Y2_sorted[i],
        #             actual[i],
        #             R[i]   # 只在第一行显示蜕变关系
        #         ])
        #
        # print(f"可视化数据已保存到: {csv_path}")
        plot_graph(module_name, Y1_sorted, Y2_sorted, actual, R)  # 绘图

        # 保存图片，输出路径
        save_plot(module_name, root_folder2_name)
        print(csv_path)
if __name__ == "__main__":
    # r="Eq(x_{21}, x_{11})"  # 输入关系r
    # R="Eq(y_{21}, y_{11})"       # 输出关系R

    r="Eq(x_{21}, x_{11}-10)"  # 输入关系r
    R="Eq(y_{21}, y_{11}-10)"       # 输出关系R

    # r="Eq(x_{21},0), Eq(x_{22},x_{12}*2-8.61077660234832)"
    # R="Eq(y_{21}, 0)"

    # # 二维
    # r="Eq(x_{21},x_{11}), Eq(x_{22},x_{12})"
    # R="Eq(y_{21}, y_{11})"

    # 一维
    # r="Eq(x_{21}, 2*pi + x_{11})"
    # R="Eq(y_{21}, y_{11})"

    # r="Eq(x_{21},x_{11}+1),Eq(x_{22},x_{12}+2),Eq(x_{23},x_{13}+3)"
    # R="Eq(1.0*y_{21},y_{11}+2*x_{11}+4*x_{12}+6*x_{13}+14)"

    # r="Eq(x_{21},x_{11}+2*pi),Eq(x_{22},x_{12}),Eq(x_{23},x_{13})"
    # R="Eq(y_{21},y_{11})"

    # r="Eq(x_{21},x_{11}+2*pi),Eq(x_{22},x_{12}),Eq(x_{23},x_{13}),Eq(x_{24},x_{14}+2*pi),Eq(x_{25},x_{15})"
    # R="Eq(y_{21},y_{11})"

    MR_Input_dimension = 1  # 待测函数输入维度
    MR_Output_dimension =1  # 待测函数输出维度
    random_count = 100       # 随机次数
    random_data_min_value = -10  # 随机生成数据的最小值
    random_data_max_value = 10   # 随机生成数据的最大值
    threshold = 1e-4# 误差

    # File_name = "Sin.py"  # SUT
    # File_name = "sum_of_digits.py"  # SUT
    # ///////////////Java
    # File_name="cos.java"
    # File_name="floor.py"


    # //////////////////
    File_name="floor.py"

    # File_name="tanh.java"
    # File_name="cos.java"
    # File_name="amax.py"
    # File_name="amin.java"
    # File_name = "Amax.py"  # SUT
    # File_name = "Three.py"  # SUT
    # File_name = "Five.py"  # SUT

    run_multidimensional(r, R, File_name, MR_Input_dimension, MR_Output_dimension, random_data_min_value, random_data_max_value, random_count,threshold)