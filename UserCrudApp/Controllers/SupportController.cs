using Microsoft.AspNetCore.Mvc;
using UserCrudApp.Services;

public class SupportController : Controller
{
    private readonly OpenAiService _ai;
    public SupportController(OpenAiService ai) => _ai = ai;

    [HttpGet]
    public IActionResult Chat() => View();

    [HttpPost]
    public async Task<IActionResult> ChatApi([FromBody] string question)
    {
        string answer = await _ai.SimpleChatBot(question);
        return Json(new { answer });
    }
}