using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Leem.Testify
{
    [TestFixture]
    public class CoverageServiceTest
    {


        [TestCase("QMedPidDataWebservice.QMED_Pid.SrchByLastNameFirstAsync(System.Object)", "Object")]
        [TestCase("Services.Util.QMedSchedulingImpersonation.ExecuteAction(System.Action)", "Action")]
        [TestCase("Services.Util.QMedSchedulingImpersonation.ExecuteAction(System.Func`1<T>)", "Func<T>")]
        [TestCase("Services.Util.DecryptionUtil.Decrypt(System.Byte[])", "Byte[]")]
        [TestCase("Services.OnlineAppointmentService.GetNextAvailableAppointments(System.Collections.Generic.IEnumerable`1<Quad.QuadMed.WebPortal.Scheduling.Objects.Provider>)", "IEnumerable<Provider>")]
        [TestCase("Services.Util.ListExtension.AddRange(System.Collections.Generic.IList`1<T>)", "IList<T>")]
        [TestCase("Services.WellnessParticipantService.GetCurrentEnrolledPrograms(System.Collections.Generic.IList`1<Quad.QuadMed.WebPortal.Domain.Objects.Interfaces.IQMedService>)", "IList<IQMedService>")]
        [TestCase("Services.WellnessParticipantService.GetProgramsEligibleToEnrollIn(System.Collections.Generic.IList`1<System.Int32>)", "IList<int>")]
        [TestCase("Services.Util.ListExtension.AddRange(System.Collections.Generic.IList`1<T>,System.Collections.Generic.IEnumerable`1<T>)", "IList<T>")]
        [TestCase("Services.MessagingService.SendAppointmentConfirmation(System.Collections.Generic.List`1<System.Web.UI.WebControls.ListItem>,System.String)", "List<ListItem>")]
        //[TestCase("Quad.QuadMed.WebPortal.Domain.Services.SurveyService.GenerateSurveyPDF(System.Nullable`1<System.Int32>)", "Nullable<int>")]
        //[TestCase("Domain.Services.SingleSignOnService.CreateSamlAssertion(System.Collections.Generic.Dictionary`2<System.String,System.String>)", "Dictionary<String,String>")]

        public void ParseArguments_HandlesString(string methodName, string expected)
        {
            var service = new CoverageService();
            var result = service.ParseArguments(methodName);

            Assert.AreEqual(expected, result[0]);
        }
        //[Test]
        //public void ParseArguments()
        //{

        //    Assert.AreEqual(true, false);
        //}

    }
}
