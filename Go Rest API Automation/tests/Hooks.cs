using Go_Rest_API_Automation.models;
using NUnit.Framework;

namespace Go_Rest_API_Automation.tests
{
    [SetUpFixture]
    public class Hooks
    {
        // massa somente leitura: fixtures que alteram estado criam o proprio usuario
        public static User SharedUser { get; private set; }

        [OneTimeSetUp]
        public void CriarUsuario()
        {
            SharedUser = UserFactory.Create("Leroy Borgan");
        }

        [OneTimeTearDown]
        public void ExcluirUsuario()
        {
            UserFactory.Delete(SharedUser.Id);
        }
    }
}
