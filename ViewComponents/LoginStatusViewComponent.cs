using Microsoft.AspNetCore.Mvc;
using _8lpets.Models;

namespace _8lpets.ViewComponents
{
    public class LoginStatusViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var user = HttpContext.Items["User"] as User;
            return View(user);
        }
    }
}
