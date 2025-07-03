namespace SimonSays.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimonSays.Messages;
using System.Threading.Tasks;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> logger;
    private readonly MessageBusService bus;

    public IndexModel(MessageBusService bus, ILogger<IndexModel> logger)
    {
        this.logger = logger;
        this.bus = bus;
    }

    public async Task<IActionResult> OnPostShowSequence()
    {
        await bus.SendAsync(new ShowSequence());
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostShowSolved()
    {
        await bus.SendAsync(new ShowSolved());
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostShowFailed()
    {
        await bus.SendAsync(new ShowFailed());
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostResetPattern()
    {
        await bus.SendAsync(new ResetPattern());
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostCaptureInput([FromBody] CaptureInputModel input)
    {
        await bus.SendAsync(new CaptureInput { ButtonNumber = input.ButtonNumber });
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostChangeDifficulty([FromBody] ChangeDifficultyModel input)
    {
        await bus.SendAsync(new ChangeDifficulty { NewDifficulty = input.NewDifficulty });
        return new JsonResult(new { success = true });
    }

    public class CaptureInputModel
    {
        public int ButtonNumber { get; set; }
    }

    public class ChangeDifficultyModel
    {
        public int NewDifficulty { get; set; }
    }
}