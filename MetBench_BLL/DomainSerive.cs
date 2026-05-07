using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class DomainSerive
    {

        private IDomainRepository Domain_Repository;

        //依赖注入实体
        public DomainSerive(IDomainRepository domain_Repository)
        {
            Domain_Repository = domain_Repository;
        }

        /// <summary>
        /// 根据应用领域名称获取应用领域
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetDomainId(string name)
        {
            var id = Domain_Repository.Get(name);
            return id;
        }

        /// <summary>
        /// 根据应用领域Id获取应用领域
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Domain GetDomain(int id)
        {
            var domain = Domain_Repository.Get(id);
            return domain;
        }

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="domain"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public int AddService(Domain domain)
        {
            if (Domain_Repository.IsDuplicate(domain,false)) 
            {
                return 1;
            }
            var result = Domain_Repository.Add(domain);
            if (result)
            {
                return 2;
            }
            else 
            {
                return 0;
            }
        }

        /// <summary>
        /// 删除服务
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool DeleteService(Domain domain)
        {
            var result = Domain_Repository.Remove(domain);
            return result;
        }

        /// <summary>
        /// 获取全部的应用领域
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Domain> showAllResult()
        {
            var result = Domain_Repository.GetAll();
            return result;
        }
        /// <summary>
        /// 更新服务
        /// </summary>
        /// <param name="domain"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public int UpdateService(Domain domain) 
        {
            if (Domain_Repository.IsDuplicate(domain, false))
            {
                return 1;
            }
            var result = Domain_Repository.Modify(domain);
            if (result)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取全部的应用领域实体
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public ObservableCollection<Domain> GetAllDomians() 
        {
            var result = Domain_Repository.GetAll();
            return result;
        }
    }
}
