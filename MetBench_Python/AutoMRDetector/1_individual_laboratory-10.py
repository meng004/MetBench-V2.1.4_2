import os
import csv
import sys
import time
import pandas as pd
import numpy as np
from datetime import datetime
import multiprocessing
import Phase1_PSOSearch
import Phase2_Filter
import Phase3_RemoveRedundancy
import settings
import settings2
from load_resulrs import load_phase3_results
import json

def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)

CONFIG_FILE = get_absolute_path("config.json")
def load_config():
    """加载配置文件 (完全不变)"""
    if not os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, 'w') as f:
            json.dump({
                "input_program_filename": None,
                "input_param_count": 1,
                "frontend_input_ranges": None,
                "frontend_input_datatypes": None,
            }, f)
    with open(CONFIG_FILE, 'r') as f:
        return json.load(f)


def save_config(config):
    """保存配置文件 (完全不变)"""
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=4)
# 阈值
threshold = 1e-7

def process_data(data):
    if isinstance(data, pd.DataFrame):
        processed_data = data.applymap(lambda x: "{:.2f}".format(x) if isinstance(x, (int, float)) else x)
    else:
        processed_data = np.where(np.logical_and(np.isfinite(data)), np.round(data, 2), data)
        processed_data = np.where(np.abs(processed_data) < threshold, 0.0, processed_data)
    return processed_data


def run_experiment(fun_id, par, i, save_root):
    # settings.func_indices = [fun_id]
    # settings.parameters_collection = [par]
    settings2.func_indices = [fun_id]
    settings2.parameters_collection = [par]
    save_root_times = save_root + '{}/{}/{}'.format(fun_id, par, i + 1)

    if not os.path.isdir(save_root_times):
        os.makedirs(save_root_times)
    # settings.output_path = save_root_times
    settings2.output_path = save_root_times

    begin_time = datetime.now()
    begin_time_str = begin_time.strftime("%H:%M:%S")

    # print('{}-{}-{} 起始时间：{}'.format(i + 1, settings.map_index_func[fun_id], par, begin_time_str))
    # print('{}-{}-{} 起始时间：{}'.format(i + 1, settings2.map_index_func[fun_id], par, begin_time_str)
    map_index_func = settings2.get_function_map()
    # print('{}-{}-{} 起始时间：{}'.format(i + 1, map_index_func[fun_id], par, begin_time_str))
    # print('start phase 1: searching ...')
    Phase1_PSOSearch.run_phase1()
    # Phase1_PSOSearch-2.run_phase1()
    # print('start phase 2: filtering ...')
    Phase2_Filter.run_phase2()
    # print('start phase 3: redundancy removing ...')
    Phase3_RemoveRedundancy.run_phase3()

    # print("=====第" + f"{i + 1}" + "次运行结果=====")
    # MRs_path =  n.output_path
    MRs_path = settings2.output_path
    # func_indices = settings.func_indices

    func_indices = settings2.func_indices
    result_files = os.listdir(f"{MRs_path}/phase3")

    for result_file in result_files:
        func_index = int(result_file[0:result_file.find('_')])
        MRs = load_phase3_results(f"{MRs_path}/phase3/{result_file}")
        for k, v in MRs.items():

            parameters_int = [int(e) for e in k.split("_")]
            NOI = parameters_int[0]
            MIR = parameters_int[1]
            MOR = parameters_int[2]
            DIR = parameters_int[3]
            DOR = parameters_int[4]

            map_relation = {1: "=", 2: ">", 3: "<"}

            html_name = str(i + 1) + f"_{k}.html"

            with open(f"{MRs_path}/" + html_name, 'a') as _file:
                j = 0
                if j == 0:
                    _file.write(f'<p>func index is {func_index} </p>')
                    _file.write(f'<p>Mode of Input Relation is "{map_relation[MIR]}" </p>')
                    _file.write(f'<p>Mode of Output Relation is "{map_relation[MOR]}" </p>')
                    _file.write(f'<p>第{i + 1}次运行结果</p>')
                    j = j + 1

                processed_input_data = process_data(v[0])
                processed_output_data = process_data(v[1])

                processed_input_data_html = pd.DataFrame(processed_input_data)
                processed_output_data_html = pd.DataFrame(processed_output_data)

                _file.write(f'<p>Input Relation is: </p>')
                _file.write(processed_input_data_html.to_html())
                _file.write(f'<p>Output Relation is: </p>')
                _file.write(processed_output_data_html.to_html())
                _file.write(r"</br>")

    end_time = datetime.now()
    end_time_str = end_time.strftime("%H:%M:%S")
    time_diff = end_time - begin_time
    spend_time = time_diff.total_seconds()
    days = int(spend_time // (3600 * 24))
    hours = int((spend_time % (3600 * 24)) // 3600)
    minutes = int((spend_time % 3600) // 60)
    seconds = int(spend_time % 60)
    time_formatted = f"{days}天 {hours}小时 {minutes}分钟 {seconds}秒"

    map_index_func = settings2.get_function_map()
    # print("{}-{}-{} 花费时间：{}\n".format(i + 1, settings.map_index_func[fun_id], par, time_formatted))
    # print("{}-{}-{} 花费时间：{}\n".format(i + 1, map_index_func[fun_id], par, time_formatted))

def main(fun_ids=None, pars=None, repet=2, num_processes=6):
    """
    执行实验的主函数

    参数:
        fun_ids (list): 要测试的函数ID列表，默认为[53]
        pars (list): 参数列表，默认为["2_1_1_1_1"]
        repet (int): 重复次数，默认为40
        num_processes (int): 进程池大小，默认为4
    """
    # print("repet的数值为：---" )
    # print(repet)
    # 设置默认参数
    if fun_ids is None:
        fun_ids = [1]
    if pars is None:
        pars = ["2_1_1_1_1"]

    # 创建输出目录
    # save_root = './output/Individual_Laboratory/{}/'.format(time.strftime("%Y_%m_%d_%Hh%Mm%Ss", time.localtime()))
    timestamp = time.strftime("%Y_%m_%d_%Hh%Mm%Ss", time.localtime())
    save_root = get_absolute_path(f'./output/Individual_Laboratory/{timestamp}/')
    if not os.path.isdir(save_root):
        os.makedirs(save_root)

    header = ['函数索引', '执行次数', '参数', '花费时间']

    # 自动设置进程数
    if num_processes is None:
        num_processes = max(1, int((os.cpu_count() or 4) * 0.75))

    for fun_id in fun_ids:
        csv_path = save_root + 'times_{}.csv'.format(fun_id)
        file_exists = os.path.isfile(csv_path)

        # 准备所有任务参数
        tasks = [(fun_id, par, i, save_root) for par in pars for i in range(repet)]

        with open(csv_path, 'a', newline='', encoding='utf-8') as f:
            cw = csv.writer(f)
            if not file_exists:
                cw.writerow(header)

            # if num_processes is None:
            #     # 根据CPU核心数自动设置进程数
            #     num_processes = min(os.cpu_count() or 4, 8)  # 最多不超过8个进程
            # 创建进程池
            # with multiprocessing.Pool(4) as pool:
            #     results = []
            #     for par in pars:
            #         for i in range(repet):
            #             # 异步提交任务
            #             result = pool.apply_async(run_experiment, args=(fun_id, par, i, save_root))
            #             results.append(result)
            #
            #     # 关闭进程池并等待所有任务完成
            #     pool.close()
            #     pool.join()
            with multiprocessing.Pool(num_processes) as pool:
                results = pool.starmap_async(run_experiment, tasks)
                pool.close()
                pool.join()

                # # 获取所有结果
                # for i, result in enumerate(results):
                #     try:
                #         time_spent = result.get()
                #         cw.writerow([fun_id, i + 1, par, time_spent])
                #     except Exception as e:
                #         print(f"任务 {i + 1} 执行失败: {str(e)}")
                #         cw.writerow([fun_id, i + 1, par, "失败"])
                # 处理结果
                for i, time_spent in enumerate(results.get()):
                    par_idx = i // repet
                    rep_idx = (i % repet) + 1
                    cw.writerow([fun_id, rep_idx, pars[par_idx], time_spent])
                    f.flush()  # 确保及时写入
    # with open("script_complete.flag", "w") as f:
    #     f.write("done")


if __name__ == '__main__':
    # 示例调用
    map_index_func = settings2.get_function_map()
    # print('字典内容:'+map_index_func[1])
    main(
        fun_ids=[1],  # 可以传入多个函数ID
        pars=["2_1_1_1_1"],  # 可以传入多个参数组合
    )
    sys.exit(0)
# 1.53