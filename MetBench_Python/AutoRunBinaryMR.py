from multidimensional  import *
import sys
#测试
# r_list = "Eq(x_{21}, 2*pi + x_{11})"
# R_list = "Eq(y_{21}, y_{11})"
# File_name = "sin.py"
# MR_Input_dimension = 1  # 待测函数输入维度
# MR_Output_dimension = 1  # 待测函数输出维度
# random_data_min_value = 1  # 随机生成数据的最小值
# random_data_max_value = 11  # 随机生成数据的最大值
# random_count = 11  # 随机次数
# threshold = 1e-4  # 误差

# r_list = "Eq(x_{21}, x_{11} + 10), Eq(x_{22}, x_{12} + 10)"
# R_list = "Eq(y_{21}, y_{11} + 10)"
# File_name = "amax.py"
# MR_Input_dimension = 2  # 待测函数输入维度
# MR_Output_dimension = 1  # 待测函数输出维度
# random_data_min_value = -10  # 随机生成数据的最小值
# random_data_max_value = 10  # 随机生成数据的最大值
# random_count = 100  # 随机次数
# threshold = 1e-4  # 误差


r_list = sys.argv[1]
R_list = sys.argv[2]
File_name = sys.argv[3]
MR_Input_dimension = int(sys.argv[4])  # 待测函数输入维度
MR_Output_dimension = int(sys.argv[5]) # 待测函数输出维度
random_data_min_value =float(sys.argv[6])   # 随机生成数据的最小值
random_data_max_value =float(sys.argv[7])   # 随机生成数据的最大值
random_count = int(sys.argv[8]) # 随机次数
threshold =float(sys.argv[9])   # 误差

run_multidimensional(r_list, R_list, File_name, MR_Input_dimension, MR_Output_dimension, random_data_min_value,
                     random_data_max_value, random_count, threshold)