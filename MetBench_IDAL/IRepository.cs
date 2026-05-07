using System.Collections.ObjectModel;

namespace MetBench_IDAL
{/// <summary>
/// 泛型接口 仓库接口 增删查改（查 是通过id）
/// </summary>
/// <typeparam name="T"></typeparam>
    public interface IRepository<T> where T : class
    {
        ObservableCollection<T> GetAll();
        //对应表的对应id
        T Get(int id);
        ObservableCollection<T> Get(T entity);
        bool Add(T entity);
        bool Modify(T entity);
        bool Remove(T entity);
    }
}
