import os
import re
import subprocess
import tempfile

# import chardet
from javalang import parse
import javalang
import jpype

def execute_java_function(java_file_folder, java_file_name, function_name, num_args, *args):
    # 拼接文件夹路径和Java文件名以获取完整路径
    java_file_full_path = os.path.join(java_file_folder, java_file_name + ".java")

    # 编译Java文件
    subprocess.check_output(["javac", java_file_full_path])

    if not jpype.isJVMStarted():
        # 如果JVM尚未启动，则启动JVM
        jpype.startJVM()

    # 添加Java类路径
    java_class_path = java_file_folder
    jpype.addClassPath(java_class_path)

    # 加载Java类
    java_class_name = java_file_name
    java_class = jpype.JClass(java_class_name)

    # 执行Java类的函数
    java_function = getattr(java_class, function_name)

    method_name, x_count, parameter_types, parameter_types_eval = get_java_method_name(java_file_full_path)

    # 如果传递给 execute_java_function 的参数个数与预期的参数数量相等
    if len(args) == num_args:

        if(parameter_types[0]=='int'):
            converted_args = [int(arg) for arg in args]  # 将参数转换为整数
        else:
            converted_args = [float(arg) for arg in args]
        result = java_function(*converted_args)
    # 如果传递给 execute_java_function 的参数个数与预期的参数数量不匹配
    else:
        raise ValueError("设置的参数数量有误！")

    return result
    jpype.shutdownJVM()  # 在返回之前关闭JVM


# 通过查找，提取方法名
def get_java_method_name(java_file_path):

    with open(java_file_path, 'r') as file:
        java_code = file.read()

        # java_code = file.read().decode('utf-8')

        # 参数个数
        comma_count = count_commas(java_code)  # 计算逗号的数量
        x_count = comma_count+1
        # print("参数个数", x_count)

        tree = parse.parse(java_code)

        # 遍历AST以查找方法定义
        for path, node in tree.filter(javalang.tree.MethodDeclaration):
            method_name = node.name

            # # 遍历方法体中的每个语句
            # for statement in node.body:
            #     if isinstance(statement, javalang.tree.Statement):
            #         break  # 找到第一个语句后跳出循环
            # 获取方法的参数类型
            parameter_types = []
            parameter_types_eval = []
            for parameter in node.parameters:
                parameter_type = parameter.type.name
                # # 用于次脚本第一个方法
                # parameter_type = eval(parameter_type)
                # # print(parameter_type)
                # 将参数类型为 double 替换为 float
                if parameter_type == 'double':
                    parameter_type = 'float'
                parameter_types.append(parameter_type)

                parameter_type_eval = eval(parameter_type)
                # print(parameter_type)
                parameter_types_eval.append(parameter_type_eval)


            # print(parameter_types)

            # 遍历方法体中的每个语句
            for statement in node.body:
                if isinstance(statement, javalang.tree.Statement):
                    if isinstance(statement, javalang.tree.ReturnStatement):
                        method_description = statement.expression
                    else:
                        method_description = statement
                    break  # 找到第一个语句后跳出循环

            # # 输出结果
            # print(f"方法名: {method_name}")
            # # print(f"返回类型: {return_type}")
            # print(f"参数类型: {parameter_types}")
        return method_name,x_count,parameter_types,parameter_types_eval

# 获取参数个数
def count_commas(text):
    # 使用正则表达式匹配小括号内的内容
    match = re.search(r"\((.*?)\)", text)

    if match:
        parameters = match.group(1)  # 获取小括号内的内容
        comma_count = parameters.count(",")  # 计算逗号的数量

        return comma_count
    else:
        return 0

def search_and_execute(java_file_name,metlib_folder,param_nums,*param):
    # 获取Java文件完整路径
    java_full_name = java_file_name+".java"
    java_file_full_path = os.path.join(metlib_folder, java_full_name)
    if os.path.exists(java_file_full_path):
        # 从Java文件中获取方法名
        # 获取参数个数param_nums
        File_name = "Add1.java"
        File_name = "Add1.java"
        method_name,p,type_name,parameter_types_eval = get_java_method_name(java_file_full_path)
        # 执行java方法
        result = execute_java_function(metlib_folder, java_file_name,method_name, param_nums,*param)  # 执行所需要的函数及参数
        return result
    else:
        print("没有找到相关文件的记录")

# 控制java解释器执行的总函数
def Run_Java_Code(Java_file_name,param_nums,*param,Java_Code_folder = os.path.join(tempfile.gettempdir(), "MetBench\MR_SUT")):
    #拼接路径
    # print(Java_Code_folder)
    # Java_Code_folder = os.path.join(tempfile.gettempdir(), "MetBench\MR_SUT")

    # Java_Code_folder = os.path.join(tempfile.gettempdir(), folder_name)
    if not os.path.exists(Java_Code_folder):
        print("找不到指定的文件夹")
    # 传值，搜索获取方法名并执行方法
    result = search_and_execute(Java_file_name, Java_Code_folder, param_nums,*param)
    return result

