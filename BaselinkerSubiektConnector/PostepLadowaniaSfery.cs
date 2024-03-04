using InsERT.Moria.Sfera;

namespace BaselinkerSubiektConnector
{
    public sealed class PostepLadowaniaSfery : IPostepLadowaniaSfery
    {
        private readonly PostepViewModel _postep;

        public PostepLadowaniaSfery(PostepViewModel postep)
        {
            _postep = postep;
        }

        public void RaportujPostep(PostepLadowaniaSferyEventArgs args)
        {
            _postep.BiezacyProcent = args.BiezacyProcent;
            _postep.Opis = args.Opis;

            Interaction.DoEvents();
        }
    }
}