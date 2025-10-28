using MarathonManager.Web.DTOs;

namespace MarathonManager.Web.ViewModels
{
    // ViewModel này chứa TẤT CẢ dữ liệu cần cho Trang chủ
    public class HomePageViewModel
    {
        public List<RaceSummaryDto> FeaturedRaces { get; set; }
        public List<BlogSummaryDto> RecentBlogPosts { get; set; }

        // Khởi tạo 2 list rỗng để tránh lỗi Null
        public HomePageViewModel()
        {
            FeaturedRaces = new List<RaceSummaryDto>();
            RecentBlogPosts = new List<BlogSummaryDto>();
        }
    }
}