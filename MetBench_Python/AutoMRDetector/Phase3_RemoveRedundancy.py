import os
import z3
import sys
import itertools
import pandas as pd
import numpy as np
from sklearn.decomposition import TruncatedSVD
import pickle
import datetime

import settings1
import settings2
import settings
import Phase1_PSOSearch


# Function to convert relative paths to absolute paths
def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)


def str_comb(vector, highest_degree):
    comb_vector = ["1"]
    degree = 1
    while degree < highest_degree + 1:
        comb_vector_with_degree = ['prod' + str(i).replace("'", "") for i in
                                   itertools.combinations_with_replacement(vector, degree)]
        comb_vector = comb_vector + comb_vector_with_degree
        degree += 1
    return comb_vector


def similarity(M1, M2, coeff_range, const_range, mutiple_modified=False):
    if not mutiple_modified:
        M1 = np.array(M1)
        M2 = np.array(M2)
    elif mutiple_modified:
        M1 = np.array(M1)
        M2 = np.array(M2)
        index_max = np.argmax(np.abs(M1))
        mutiple_factor = np.divide(M2[index_max], M1[index_max])
        M2 = np.multiply(M2, mutiple_factor)

    coeff_range = np.array(coeff_range)
    const_range = np.array(const_range)

    const_M1 = M1[..., 0:1]
    const_M2 = M2[..., 0:1]
    coeff_M1 = M1[..., 1:]
    coeff_M2 = M2[..., 1:]
    range_coeff = np.abs(coeff_range[1] - coeff_range[0])
    range_const = np.abs(const_range[1] - const_range[0])

    sim_const = np.divide(np.sqrt(np.sum(np.square(const_M1 - const_M2))), range_const)
    sim_coeff = np.divide(np.sqrt(np.sum(np.square(coeff_M1 - coeff_M2))), range_coeff)

    sim = np.divide(np.add(sim_coeff, sim_const), np.prod(M1.shape))
    return sim


def isRedundant(no_of_ele_input, no_of_ele_output, no_of_inputs, mode_input_relation, mode_output_relation,
                degree_of_input_relation, degree_of_output_relation, A1, B1, A2, B2, coeff_range, const_range):
    similarity_thre = 0.05

    def isInputRd():
        if mode_input_relation == 1:
            if similarity(A1, A2, coeff_range, const_range) < similarity_thre:
                return True
            else:
                return False
        elif mode_input_relation in [2, 3]:
            if similarity(A1, A2, coeff_range, const_range) < similarity_thre:
                return True
            else:
                i0 = []
                i_element = 1
                while i_element <= no_of_ele_input:
                    x_i = z3.Real(f"x_{i_element}")
                    i0.append(x_i)
                    i_element += 1

                i0 = np.array(i0)
                comb_i0 = Phase1_PSOSearch.comb(i0, degree_of_input_relation)
                i1_to_end_A1 = np.dot(A1, comb_i0)
                i1_to_end_A2 = np.dot(A2, comb_i0)

                if mode_input_relation == 2:
                    isInputRd_all_1_to_end_inputs = []
                    for i_input in range(no_of_inputs - 1):
                        i1_to_end_A1_i_input = i1_to_end_A1[i_input]
                        i1_to_end_A2_i_input = i1_to_end_A2[i_input]

                        isInputRd_i_input = []
                        for i_element in range(no_of_ele_input):
                            s = z3.Solver()
                            s.add(i1_to_end_A1_i_input[i_element] > i1_to_end_A2_i_input[i_element])
                            if s.check() == z3.unsat:
                                isInputRd_i_input.append(1)
                            else:
                                return False
                        if np.product(isInputRd_i_input):
                            isInputRd_all_1_to_end_inputs.append(1)
                        else:
                            return False
                    if np.product(isInputRd_all_1_to_end_inputs):
                        return True
                    else:
                        return False

                elif mode_input_relation == 3:
                    isInputRd_all_1_to_end_inputs = []
                    for i_input in range(no_of_inputs - 1):
                        i1_to_end_A1_i_input = i1_to_end_A1[i_input]
                        i1_to_end_A2_i_input = i1_to_end_A2[i_input]

                        isInputRd_i_input = []
                        for i_element in range(no_of_ele_input):
                            s = z3.Solver()
                            s.add(i1_to_end_A1_i_input[i_element] < i1_to_end_A2_i_input[i_element])
                            if s.check() == z3.unsat:
                                isInputRd_i_input.append(1)
                            else:
                                return False
                        if np.product(isInputRd_i_input):
                            isInputRd_all_1_to_end_inputs.append(1)
                        else:
                            return False
                    if np.product(isInputRd_all_1_to_end_inputs):
                        return True
                    else:
                        return False

    def isOutputRd():
        if mode_output_relation == 1:
            if similarity(B1, B2, coeff_range, const_range, mutiple_modified=True) < similarity_thre:
                return True
            else:
                return False
        elif mode_output_relation in [2, 3]:
            if similarity(B1, B2, coeff_range, const_range, mutiple_modified=True) < similarity_thre:
                return True
            else:
                s2 = z3.Solver()

                outputs = []
                i = 1
                while i <= (no_of_ele_output * no_of_inputs):
                    outputs_i = z3.Real('y_{}'.format(i))
                    outputs.append(outputs_i)
                    i += 1

                outputs = np.array(outputs)
                y = Phase1_PSOSearch.comb(outputs, degree_of_output_relation)
                output_relation1 = np.dot(B1, y)
                output_relation2 = np.dot(B2, y)

                if mode_output_relation == 2:
                    s2.add(output_relation1 > output_relation2)
                    if s2.check() == z3.unsat:
                        return True
                    else:
                        return False

                elif mode_output_relation == 3:
                    s2.add(output_relation1 < output_relation2)
                    if s2.check() == z3.unsat:
                        return True
                    else:
                        return False

    if mode_input_relation == 1:
        if mode_output_relation == 1:
            return isInputRd()
        elif mode_output_relation in [2, 3]:
            return isInputRd() and isOutputRd()
    elif mode_input_relation in [2, 3]:
        return isInputRd() and isOutputRd()


def z3_check(no_of_ele_input, no_of_ele_output, no_of_inputs, mode_input_relation, mode_output_relation,
             degree_of_input_relation, degree_of_output_relation, A_candidates, B_candidates, coeff_range, const_range):
    before_z3 = A_candidates.shape[0]

    size = A_candidates.shape[0]
    indices_Red = []

    index_base = 0
    while index_base < size:
        if index_base in indices_Red:
            index_base += 1
            continue
        else:
            index_compare = 0
            while index_compare < size:
                if (index_compare in indices_Red) or (index_compare == index_base):
                    index_compare += 1
                    continue
                else:
                    if isRedundant(no_of_ele_input, no_of_ele_output, no_of_inputs, mode_input_relation,
                                   mode_output_relation, degree_of_input_relation, degree_of_output_relation,
                                   A_candidates[index_base], B_candidates[index_base], A_candidates[index_compare],
                                   B_candidates[index_compare], coeff_range, const_range):
                        indices_Red.append(index_compare)
                index_compare += 1

        index_base += 1

    A_candidates_after_z3 = np.delete(A_candidates, indices_Red, axis=0)
    B_candidates_after_z3 = np.delete(B_candidates, indices_Red, axis=0)
    after_z3 = A_candidates_after_z3.shape[0]
    return A_candidates_after_z3, B_candidates_after_z3, before_z3, after_z3


def svd_check(MRs, NEOI, NEOO):
    hNOI = 0
    hDIR = 0
    hDOR = 0
    for parameters in MRs.keys():
        parameters_int = [int(e) for e in parameters.split("_")]
        hNOI = max(hNOI, parameters_int[0])
        hDIR = max(hDIR, parameters_int[3])
        hDOR = max(hDOR, parameters_int[4])

    x_all = {}
    hu = str_comb([f"i0_{i + 1}" for i in range(NEOI)], hDIR)

    MR_all = []
    for parameters, AB_after_CS in MRs.items():
        parameters_int = [int(e) for e in parameters.split("_")]
        NOI = parameters_int[0]
        MIR = parameters_int[1]
        MOR = parameters_int[2]
        DIR = parameters_int[3]
        DOR = parameters_int[4]

        As = AB_after_CS[0]
        Bs = AB_after_CS[1]

        u = str_comb([f"i0_{i + 1}" for i in range(NEOI)], DIR)

        for i_A in range(As.shape[0]):
            A = As[i_A]
            o_orig = ["o0"]

            for i_NOI in range(A.shape[0]):
                A_iNOI = A[i_NOI]
                x_temp = pd.DataFrame(columns=hu)
                for i_EOI in range(NEOI):
                    x_temp_iEOI = pd.DataFrame([A_iNOI[i_EOI]], columns=u, index=[f"e{i_EOI + 1}"])
                    x_temp = pd.concat([x_temp, x_temp_iEOI], ignore_index=False, sort=False)
                    x_temp = x_temp.fillna(0)
                isNew = True
                for x, A_x in x_all.items():
                    isExist = np.allclose(A_x, x_temp.values, atol=0.05, rtol=0.1, equal_nan=True)
                    if isExist:
                        o_orig.append(f"o{x}")
                        isNew = False
                        break
                if isNew:
                    number_of_x = len(x_all)
                    x_all[f"i{number_of_x + 1}"] = x_temp.values
                    o_orig.append(f"o{number_of_x + 1}")

            o = []
            for i in range(len(o_orig)):
                for i_ele in range(NEOO):
                    o.append(f"{o_orig[i]}_{i_ele + 1}")

            v = str_comb(o, DOR)
            MR = pd.DataFrame([Bs[i_A]], columns=v)
            MR = MR.groupby(MR.columns, axis=1).sum()
            MR_all.append(MR)

    MR_all_df = pd.concat((df for df in MR_all), ignore_index=True, sort=True)
    y_all = MR_all_df.columns
    MR_all_df = MR_all_df.fillna(0)
    MR_all_array = MR_all_df.values

    c = MR_all_array.T
    svd = TruncatedSVD(n_components=(c.shape[1] - 1))
    svd.fit(c)
    c_svd = svd.transform(c)
    c_after_svd = c_svd.T
    ratios = np.round(svd.explained_variance_ratio_, 2)

    MRs_matrix_after_svd = []
    accu_ration = 0

    for i in range(len(ratios)):
        MRs_matrix_after_svd.append(c_after_svd[i])
        accu_ration += ratios[i]

        if accu_ration > 0.99:
            break

    MRs_df_after_svd = pd.DataFrame(MRs_matrix_after_svd, columns=y_all)

    return x_all, MRs_df_after_svd, hDIR


def phase3(folder_path, func_indices, coeff_range, const_range):
    abs_folder_path = get_absolute_path(folder_path)
    phase3_path = os.path.join(abs_folder_path, "phase3")
    phase2_path = os.path.join(abs_folder_path, "phase2")
    counts_file = os.path.join(abs_folder_path, "counts.csv")
    performance_file = os.path.join(abs_folder_path, "performance.csv")

    if not os.path.isdir(phase3_path):
        os.mkdir(phase3_path)

    if os.path.isfile(counts_file):
        results = pd.read_csv(counts_file, index_col=0)
    else:
        results = pd.DataFrame()

    if os.path.isfile(performance_file):
        times = pd.read_csv(performance_file, index_col=0)
    else:
        times = pd.DataFrame()

    for func_index in func_indices:
        stats_after_cs_svd_df = {}
        time_cs_svd = {}

        NEI = settings2.getNEI(func_index)
        NEO = settings2.getNEO(func_index)

        AB_all = {}
        for filename in os.listdir(phase2_path):
            if filename.startswith(f"{func_index}_") and filename.endswith("after_filter.npz"):
                parameters = filename[-26:-17]
                time_cs_svd[parameters] = 0
                mr_file = os.path.join(phase2_path, filename)
                MRs = np.load(mr_file)
                As = MRs["A_candidates"]
                Bs = MRs["B_candidates"]
                if As.shape[0] > 1:
                    AB_all[parameters] = [As, Bs]
                elif As.shape[0] == 1:
                    stats_after_cs_svd_df[parameters] = 1
                    output_file = os.path.join(phase3_path, f'{func_index}_{parameters}_after_cs_svd.npz')
                    np.savez(output_file, A_candidates=As, B_candidates=Bs)
                else:
                    stats_after_cs_svd_df[parameters] = 0

        AB_all_after_CS = {}
        MRs_each_type_after_cs_svd = {}
        for parameters, ABs in AB_all.items():
            t1 = datetime.datetime.now()

            parameters_int = [int(e) for e in parameters.split("_")]
            NOI = parameters_int[0]
            MIR = parameters_int[1]
            MOR = parameters_int[2]
            DIR = parameters_int[3]
            DOR = parameters_int[4]

            A_candidates = ABs[0]
            B_candidates = ABs[1]

            A_candidates_after_CS, B_candidates_after_CS, before_CS, after_CS = z3_check(
                NEI, NEO, NOI, MIR, MOR, DIR, DOR,
                A_candidates, B_candidates, coeff_range, const_range
            )
            AB_all_after_CS[parameters] = [A_candidates_after_CS, B_candidates_after_CS]

            output_file = os.path.join(phase3_path, f'{func_index}_{parameters}_after_cs_svd.npz')
            np.savez(
                output_file,
                A_candidates=A_candidates_after_CS,
                B_candidates=B_candidates_after_CS
            )

            if parameters_int[2] != 1:
                stats_after_cs_svd_df[parameters] = A_candidates_after_CS.shape[0]
            else:
                if A_candidates_after_CS.shape[0] > 1:
                    MRs_each_type_after_cs_svd[parameters] = list(
                        svd_check({parameters: AB_all_after_CS[parameters]}, NEI, NEO)
                    )
                    stats_after_cs_svd_df[parameters] = MRs_each_type_after_cs_svd[parameters][1].shape[0]
                else:
                    stats_after_cs_svd_df[parameters] = A_candidates_after_CS.shape[0]

            t2 = datetime.datetime.now()
            cost_time = np.round((t2 - t1).total_seconds(), 2)
            time_cs_svd[parameters] = time_cs_svd[parameters] + cost_time

        if len(MRs_each_type_after_cs_svd) > 0:
            output_file = os.path.join(phase3_path, f"{func_index}_MRs_other_types_after_cs_svd.pkl")
            with open(output_file, "wb") as f1:
                pickle.dump(MRs_each_type_after_cs_svd, f1, pickle.HIGHEST_PROTOCOL)

        for parameters, number in stats_after_cs_svd_df.items():
            results.loc[f"{func_index}_{parameters}", "phase3"] = number
        results.to_csv(counts_file)

        for parameters, time in time_cs_svd.items():
            times.loc[f"{func_index}_{parameters}", "phase3"] = time
        times.to_csv(performance_file)


def checkAfterSVD(folder_path, func_indices):
    abs_folder_path = get_absolute_path(folder_path)
    counts_file = os.path.join(abs_folder_path, "counts.csv")
    phase3_path = os.path.join(abs_folder_path, "phase3")

    filer_phase3_df = pd.read_csv(counts_file, index_col=0)

    for func_index in func_indices:
        no_of_elements_output = settings2.getNEO(func_index)
        no_of_elements_input = settings2.getNEI(func_index)
        inputcases_range = settings2.get_input_range(func_index)
        no_of_testcases = 100

        MRs_types = os.listdir(phase3_path)
        for MRs_type in MRs_types:
            if MRs_type.startswith(f"{func_index}_") and MRs_type.endswith("group_after_cs_svd.pkl"):
                i0_all = Phase1_PSOSearch.generate_i0_all(
                    settings2.get_input_datatype(func_index),
                    settings2.get_input_range(func_index),
                    no_of_testcases
                )

                pkl_file = os.path.join(phase3_path, MRs_type)
                with open(pkl_file, "rb") as f:
                    MRs_dict = pickle.load(f)

                for parameters, MRs in MRs_dict.items():
                    filer_phase3 = 0
                    x_all_dict = MRs[0]
                    y_all_df = MRs[1]
                    hDIR = MRs[2]

                    y_o_isKill_df = pd.DataFrame()
                    for index_i0 in range(i0_all.shape[0]):
                        i0 = i0_all[index_i0]
                        u = Phase1_PSOSearch.comb(i0, hDIR)
                        x_value_dict = {}
                        y_element_value_dict = {}
                        for x_name, A in x_all_dict.items():
                            x = np.dot(A, u)
                            x_value_dict[x_name] = x
                            y = settings2.program(x, func_index)
                            for index_eo in range(no_of_elements_output):
                                y_element_value_dict[f"f{x_name}_{index_eo + 1}"] = y[index_eo]

                        y0 = settings2.program(i0, func_index)
                        for index_eo in range(no_of_elements_output):
                            y_element_value_dict[f"fx0_{index_eo + 1}"] = y0[index_eo]

                        y_all_names = y_all_df.columns.values
                        y_all_values = np.zeros(y_all_names.shape)
                        for index_y in range(y_all_names.shape[0]):
                            y_names = list(y_all_names[index_y])
                            y_elements = []
                            for ii in range(len(y_names)):
                                try:
                                    y_elements.append(float(y_names[ii]))
                                except:
                                    y_elements.append(y_element_value_dict[y_names[ii]])
                            y_all_values[index_y] = np.product(y_elements)

                        for index_MR in range(y_all_df.shape[0]):
                            B = y_all_df.iloc[index_MR, :].values
                            Bv = np.dot(B, y_all_values)
                            if np.isreal(Bv) and not np.isnan(Bv):
                                if np.abs(Bv) < 0.1:
                                    y_o_isKill_df.loc[index_MR, index_i0] = 0
                                else:
                                    y_o_isKill_df.loc[index_MR, index_i0] = 1
                            else:
                                y_o_isKill_df.loc[index_MR, index_i0] = 1

                    for index_MR in range(y_o_isKill_df.shape[0]):
                        kill_o_number = np.sum(y_o_isKill_df.iloc[index_MR, :].values)
                        cost_o = np.divide(kill_o_number, no_of_testcases)
                        if cost_o < 0.05:
                            filer_phase3 += 1
                        else:
                            MRs_dict[parameters][1] = MRs_dict[parameters][1].drop([index_MR])

                    filer_phase3_df.loc[f"{func_index}_{parameters}", "phase3"] = filer_phase3

                with open(os.path.join(phase3_path, f"{func_index}_MRs_group_after_cs_svd.pkl"), "wb") as f2:
                    pickle.dump(MRs_dict, f2, pickle.HIGHEST_PROTOCOL)

            elif MRs_type.startswith(f"{func_index}_") and MRs_type.endswith("other_types_after_cs_svd.pkl"):
                i0_all = Phase1_PSOSearch.generate_i0_all(
                    settings2.get_input_datatype(func_index),
                    settings2.get_input_range(func_index),
                    no_of_testcases
                )

                pkl_file = os.path.join(phase3_path, MRs_type)
                with open(pkl_file, "rb") as f:
                    MRs_dict = pickle.load(f)

                for parameters, MRs in MRs_dict.items():
                    filer_phase3 = 0
                    parameters_int = [int(e) for e in parameters.split("_")]
                    no_of_inputs = parameters_int[0]
                    mode_input_relation = parameters_int[1]
                    mode_output_relation = parameters_int[2]
                    degree_of_input_relation = parameters_int[3]
                    degree_of_output_relation = parameters_int[4]

                    x_all_dict = MRs[0]
                    y_all_df = MRs[1]

                    y_o_isKill_df = pd.DataFrame()

                    for index_i0 in range(i0_all.shape[0]):
                        i0 = i0_all[index_i0]
                        u = Phase1_PSOSearch.comb(i0, degree_of_input_relation)
                        x_value_dict = {}
                        y_element_value_dict = {}
                        for x_name, A in x_all_dict.items():
                            x = np.dot(A, u)
                            x_value_dict[x_name] = x
                            y = settings2.program(x, func_index)
                            for index_eo in range(no_of_elements_output):
                                y_element_value_dict[f"f{x_name}_{index_eo + 1}"] = y[index_eo]

                        y0 = settings2.program(i0, func_index)
                        for index_eo in range(no_of_elements_output):
                            y_element_value_dict[f"fx0_{index_eo + 1}"] = y0[index_eo]

                        y_all_names = y_all_df.columns.values
                        y_all_values = np.zeros(y_all_names.shape)
                        for index_y in range(y_all_names.shape[0]):
                            y_names = list(y_all_names[index_y])
                            y_elements = []
                            for ii in range(len(y_names)):
                                try:
                                    y_elements.append(float(y_names[ii]))
                                except:
                                    y_elements.append(y_element_value_dict[y_names[ii]])
                            y_all_values[index_y] = np.product(y_elements)

                        for index_MR in range(y_all_df.shape[0]):
                            B = y_all_df.iloc[index_MR, :].values
                            Bv = np.dot(B, y_all_values)
                            if np.isreal(Bv) and not np.isnan(Bv):
                                if np.abs(Bv) < 0.1:
                                    y_o_isKill_df.loc[index_MR, index_i0] = 0
                                else:
                                    y_o_isKill_df.loc[index_MR, index_i0] = 1
                            else:
                                y_o_isKill_df.loc[index_MR, index_i0] = 1

                    for index_MR in range(y_o_isKill_df.shape[0]):
                        kill_o_number = np.sum(y_o_isKill_df.iloc[index_MR, :].values)
                        cost_o = np.divide(kill_o_number, no_of_testcases)
                        if cost_o < 0.05:
                            filer_phase3 += 1
                        else:
                            MRs_dict[parameters][1] = MRs_dict[parameters][1].drop([index_MR])

                    filer_phase3_df.loc[f"{func_index}_{parameters}", "phase3"] = filer_phase3

                with open(os.path.join(phase3_path, f"{func_index}_MRs_other_types_after_cs_svd.pkl"), "wb") as f2:
                    pickle.dump(MRs_dict, f2, pickle.HIGHEST_PROTOCOL)

    # filer_phase3_df.to_csv(counts_file)


def run_phase3():
    folder_path = get_absolute_path(settings2.output_path)
    func_indices = settings2.func_indices
    const_range = settings2.const_range
    coeff_range = settings2.coeff_range

    phase3(folder_path, func_indices, coeff_range, const_range)


if __name__ == '__main__':
    run_phase3()