using ncoded.NetStandard.InversionOfControl;
using System;
using System.Linq;

namespace NewsBaker
{
    class RestServerIoCWrapper : NETStandard.RestServer.IRestServerServiceDependencyResolver
    {
        private readonly IDependencyResolver _container;
        public RestServerIoCWrapper(IDependencyResolver container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public object[] GetDependecys(Type[] dependencyTypes)
        {
            return dependencyTypes.Select(type => GetDependency(type)).ToArray();
        }

        public object GetDependency(Type dependencyType)
        {
            return _container.GetDependency(dependencyType);
        }
    }
}
