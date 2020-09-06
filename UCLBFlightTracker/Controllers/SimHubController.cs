using Microsoft.AspNetCore.Mvc;

namespace UCLBFlightTracker.Controllers
{
    public class SimHubController : ControllerBase
    {
        private readonly UCLBClient client;

        public SimHubController(UCLBClient client)
        {
            this.client = client;
        }

        [HttpPost("initPlugin")]
        public ActionResult<bool> InitPlugin()
        {
            client.Initialize();
            return true;
        }
    }
}
