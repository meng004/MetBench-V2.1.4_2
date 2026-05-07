import subprocess
import time
from queue import Queue
from threading import Thread
import sys
import settings1
import json
import os

CONFIG_FILE = "config.json"
def worker(q):
    """工作线程，从队列中获取并执行任务"""
    while True:
        script = q.get()
        if script is None:  # 终止信号
            break

        print(f"\n开始执行 {script}...")
        try:
            result = subprocess.run(["python", script], check=True)
            print(f"{script} 执行完成")
        except subprocess.CalledProcessError as e:
            print(f"脚本 {script} 执行失败: {e}")
        finally:
            q.task_done()


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

def main():
    config = load_config()
    config["input_program_filename"] = "sin.py"
    config["input_param_count"] = 1
    config["frontend_input_ranges"] = "(10,20)"
    config["frontend_input_datatypes"] = 'float'
    save_config(config)

    settings1.process_frontend_inputs()

    print('字典内容：'+settings1.map_index_func[1]);
    # args = sys.argv[1:]  # 跳过脚本名
    # # 1. 首先处理前端输入
    # print("正在处理前端输入...")
    # frontend_params = {
    #     'filename': args[0],
    #     'param_count': int(args[1]),
    #     'range_str': args[2],
    #     'datatype_str': args[3]
    # }
    #

    # frontend_params = {
    #     'filename': 'sin.py',
    #     'param_count': int('1'),
    #     'range_str': '(10,20)',
    #     'datatype_str': 'float'
    # }

    # 1. 处理前端输入
    # process_result = settings.process_frontend_inputs(
    #     filename=frontend_params['filename'],
    #     param_count=frontend_params['param_count'],
    #     range_str=frontend_params['range_str'],
    #     datatype_str=frontend_params['datatype_str']
    # )
    #
    # if not process_result['final_status']:
    #     print(f"前端输入处理失败: {process_result.get('error', '未知错误')}")
    #     return

    # 创建任务队列
    q = Queue()

    # 定义执行顺序
    scripts = [
        "1_individual_laboratory-9.py",
        "2_npz2md-1.py",
        "3_npcsv_many-14.py",
        "4-redundancy_removal-2.py",
        "5-mt_test-many-13.py"
    ]

    # 启动工作线程
    t = Thread(target=worker, args=(q,))
    t.start()

    # 添加任务到队列
    for script in scripts:
        q.put(script)

    # 等待所有任务完成
    q.join()

    # 发送终止信号
    q.put(None)
    t.join()

    print("\n所有脚本执行完成")


if __name__ == "__main__":
    main()

# import subprocess
# import time
# from queue import Queue
# from threading import Thread, Lock
# import sys
# import settings
# import pickle
# import os
#
#
# class GlobalConfigManager:
#     _instance = None
#     _lock = Lock()
#
#     def __new__(cls):
#         with cls._lock:
#             if cls._instance is None:
#                 cls._instance = super().__new__(cls)
#                 cls._instance._initialized = False
#         return cls._instance
#
#     def __init__(self):
#         if not self._initialized:
#             self.config = {
#                 'input_program_filename': None,
#                 'input_param_count': 1,
#                 'frontend_input_ranges': None,
#                 'frontend_input_datatypes': None
#             }
#             self._initialized = True
#             self._lock = Lock()
#
#     def update_config(self,** kwargs):
#         with self._lock:
#             self.config.update(kwargs)
#             # 自动保存到文件
#             self._save_config()
#
#     def get_config(self, key):
#         with self._lock:
#             return self.config.get(key)
#
#     def _save_config(self):
#         with self._lock:
#             with open('global_config.pkl', 'wb') as f:
#                 pickle.dump(self.config, f)
#
#     def load_config(self):
#         if os.path.exists('global_config.pkl'):
#             with self._lock:
#                 with open('global_config.pkl', 'rb') as f:
#                     self.config = pickle.load(f)
#         return self.config
#
#
# # 创建全局配置管理器单例
# config_manager = GlobalConfigManager()
#
#
# def worker(q):
#     """工作线程，从队列中获取并执行任务"""
#     while True:
#         script = q.get()
#         if script is None:  # 终止信号
#             break
#
#         print(f"\n开始执行 {script}...")
#         try:
#             # 在执行前加载最新配置
#             current_config = config_manager.load_config()
#             print(f"当前配置: {current_config}")
#
#             result = subprocess.run(["python", script], check=True)
#             print(f"{script} 执行完成")
#         except subprocess.CalledProcessError as e:
#             print(f"脚本 {script} 执行失败: {e}")
#         except Exception as e:
#             print(f"处理脚本 {script} 时发生意外错误: {e}")
#         finally:
#             q.task_done()
#
#
# def process_frontend_inputs(frontend_params):
#     """处理前端输入并更新全局配置"""
#     try:
#         # 1. 处理前端输入
#         process_result = settings.process_frontend_inputs(
#             filename=frontend_params['filename'],
#             param_count=frontend_params['param_count'],
#             range_str=frontend_params['range_str'],
#             datatype_str=frontend_params['datatype_str']
#         )
#
#         if not process_result['final_status']:
#             print(f"前端输入处理失败: {process_result.get('error', '未知错误')}")
#             return False
#
#         # 2. 更新全局配置管理器
#         config_manager.update_config(
#             input_program_filename=frontend_params['filename'],
#             input_param_count=frontend_params['param_count'],
#             frontend_input_ranges=settings.frontend_input_ranges,
#             frontend_input_datatypes=settings.frontend_input_datatypes
#         )
#
#         return True
#     except Exception as e:
#         print(f"更新全局配置时出错: {e}")
#         return False
#
#
# def main():
#     # 示例前端参数
#     frontend_params = {
#         'filename': 'sin.py',
#         'param_count': int('1'),
#         'range_str': '(10,20)',
#         'datatype_str': 'float'
#     }
#
#     # 处理前端输入并初始化全局配置
#     if not process_frontend_inputs(frontend_params):
#         return
#
#     # 创建任务队列
#     q = Queue()
#
#     # 定义执行顺序
#     scripts = [
#         "1_individual_laboratory-9.py",
#         "2_npz2md-1.py",
#         "3_npcsv_many-14.py",
#         "4-redundancy_removal-2.py",
#         "5-mt_test-many-13.py"
#     ]
#
#     # 启动工作线程
#     threads = []
#     for _ in range(2):  # 使用2个工作线程并行执行
#         t = Thread(target=worker, args=(q,))
#         t.start()
#         threads.append(t)
#
#     # 添加任务到队列
#     for script in scripts:
#         q.put(script)
#
#     # 等待所有任务完成
#     q.join()
#
#     # 发送终止信号
#     for _ in range(len(threads)):
#         q.put(None)
#
#     for t in threads:
#         t.join()
#
#     print("\n所有脚本执行完成")
#
#     # 清理配置文件
#     try:
#         os.remove('global_config.pkl')
#     except:
#         pass
#
#
# if __name__ == "__main__":
#     main()