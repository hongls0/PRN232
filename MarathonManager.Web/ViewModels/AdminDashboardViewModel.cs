// Trong MarathonManager.Web/ViewModels/AdminDashboardViewModel.cs
using MarathonManager.Web.DTOs;
using X.PagedList;

namespace MarathonManager.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public IPagedList<RaceSummaryDto> PendingRaces { get; set; }
        public IPagedList<UserDto> AllUsers { get; set; }
        public IPagedList<BlogAdminDto> AllBlogPosts { get; set; } // <-- THÊM DÒNG NÀY
    }
}