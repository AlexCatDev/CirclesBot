using Discord;
using Discord.Rest;
using System.Collections.Generic;

namespace CirclesBot
{
    public class Pages
    {
        public RestUserMessage MessageHandle;

        private List<Embed> pages = new List<Embed>();

        public int PageCount => pages.Count;

        private int pageIndex = 0;

        public void AddEmbed(Embed embed)
        {
            pages.Add(embed);
        }

        public Embed GetFirst => pages[0];

        public Embed GetCurrentPage => pages[pageIndex];

        public void GoForwards()
        {
            int previousIndex = pageIndex;

            pageIndex = Extensions.Clamp(pageIndex + 1, 0, pages.Count - 1);

            if (previousIndex != pageIndex)
                updateMessage();
        }

        public void GoBackwards()
        {
            int previousIndex = pageIndex;

            pageIndex = Extensions.Clamp(pageIndex - 1, 0, pages.Count - 1);

            if (previousIndex != pageIndex)
                updateMessage();
        }

        private void updateMessage()
        {
            MessageHandle.ModifyAsync(a => a.Embed = GetCurrentPage);
        }
    }
}
