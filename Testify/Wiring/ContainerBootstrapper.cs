using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructureMap;
using Leem.Testify.DataLayer;
using Leem.Testify.Domain.DaoInterfaces;


namespace Leem.Testify.Wiring
{
    public  class ContainerBootstrapper
    {

        public  void BootstrapStructureMap()
        {

            // Initialize the static ObjectFactory container
            ObjectFactory.Initialize(x => x.Scan(scan =>
                    {
                        scan.WithDefaultConventions();
                        scan.Assembly("DataLayer");
                        scan.Assembly("Domain");
                    }));
            ObjectFactory.Configure(x=>
            {
                    x.For<ITestifyQueries>().Use<TestifyQueries>();

                });


        }
    }
}
