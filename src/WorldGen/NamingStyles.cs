using System.Collections.Generic;
using System.Text;

namespace WorldGen
{
    public sealed class SyllableGen
    {
        public List<string[]> Patterns;                 // e.g. ["prefix","middle","suffix"]
        public Dictionary<string, string[]> Parts;      // "prefix" -> [...], etc.
    }

    public sealed class NamingStyle
    {
        public string Id, DisplayName;
        public string[] FactionNames;
        public SyllableGen Settlements;
        public SyllableGen Rivers;
    }

    /// <summary>
    /// World Naming Styles (Naming_Style_System_v2.md). Faction names are drawn
    /// from curated pools; settlements/rivers are generated from syllables.
    /// Style is assigned to a faction by a hybrid rule (values, else biome).
    /// </summary>
    public static class NamingData
    {
        private static readonly List<string[]> P3 = new List<string[]>
        {
            new[] { "prefix", "middle", "suffix" },
            new[] { "prefix", "suffix" }
        };
        private static readonly List<string[]> P2 = new List<string[]>
        {
            new[] { "prefix", "suffix" }
        };

        private static SyllableGen S(string[] pre, string[] mid, string[] suf) => new SyllableGen
        { Patterns = P3, Parts = new Dictionary<string, string[]> { { "prefix", pre }, { "middle", mid }, { "suffix", suf } } };

        private static SyllableGen R(string[] pre, string[] suf) => new SyllableGen
        { Patterns = P2, Parts = new Dictionary<string, string[]> { { "prefix", pre }, { "suffix", suf } } };

        public static readonly List<NamingStyle> Styles = new List<NamingStyle>
        {
            new NamingStyle { Id="coastal_flowing", DisplayName="Coastal Flowing",
                FactionNames=new[]{"Aruvena","Marakai","Taloren","Selovar","Nerala"},
                Settlements=S(new[]{"Ta","Me","Ne","Sa","Ko","Aru"}, new[]{"lo","ra","ve","na","ri"}, new[]{"a","en","or","is","um"}),
                Rivers=R(new[]{"Na","Se","Mor","Tal","Aru"}, new[]{"el","ar","in","os","a"}) },
            new NamingStyle { Id="highland_hard", DisplayName="Highland Hard",
                FactionNames=new[]{"Kareth","Dorkan","Brekhan","Karvek","Tordun"},
                Settlements=S(new[]{"Kar","Dor","Brak","Tor","Vak","Dran"}, new[]{"kr","dr","gr","kh"}, new[]{"en","ak","or","un","ek"}),
                Rivers=R(new[]{"Dur","Kor","Var","Drak","Tor"}, new[]{"an","ek","ur","or","ath"}) },
            new NamingStyle { Id="hidden_whisper", DisplayName="Hidden Whisper",
                FactionNames=new[]{"Nhal-Seth","Eshran","Vel-Nar","Ushai","Sethra"},
                Settlements=S(new[]{"Nhal","Esh","Vel","Ush","Seth"}, new[]{"a","e","sh","i"}, new[]{"ar","eth","ai","ir","un"}),
                Rivers=R(new[]{"Ish","Nha","Vel","Seth","Ura"}, new[]{"en","ai","ur","eth","il"}) },
            new NamingStyle { Id="desert", DisplayName="Desert",
                FactionNames=new[]{"Sahariq","Zareen","Qasira","Azrakh","Neshara"},
                Settlements=S(new[]{"Sa","Za","Qa","Az","Ne"}, new[]{"har","zir","mir","rah"}, new[]{"iq","a","een","ar","un"}),
                Rivers=R(new[]{"Zah","Mir","Sar","Az"}, new[]{"ir","al","esh","un","a"}) },
            new NamingStyle { Id="forest", DisplayName="Forest Soft",
                FactionNames=new[]{"Elarin","Sylvara","Lethien","Valeth","Mirelai"},
                Settlements=S(new[]{"Ela","Syl","Leth","Mir","Vale"}, new[]{"ra","li","na","ve"}, new[]{"n","el","a","is","or"}),
                Rivers=R(new[]{"Ela","Syl","Mir","Leth"}, new[]{"rin","iel","ara","en","is"}) },
            new NamingStyle { Id="swamp", DisplayName="Swamp",
                FactionNames=new[]{"Gruk","Mav","Skor","Dren","Ulm"},
                Settlements=S(new[]{"Gr","Sk","Dr","Ul","Br"}, new[]{"a","o"}, new[]{"uk","esh","or","am","ik"}),
                Rivers=R(new[]{"Mud","Gr","Ul"}, new[]{"ak","um","esh","or"}) },
            new NamingStyle { Id="steppe", DisplayName="Steppe",
                FactionNames=new[]{"Orunai","Tegara","Bairuk","Khalan","Suren"},
                Settlements=S(new[]{"Oru","Tega","Bai","Kha","Su"}, new[]{"na","ra","lu"}, new[]{"i","an","uk","ar"}),
                Rivers=R(new[]{"Oru","Bai","Sur"}, new[]{"al","un","ar","ei"}) },
            new NamingStyle { Id="island", DisplayName="Island",
                FactionNames=new[]{"Aoloa","Mairua","Keanoa","Taviri","Oanaki"},
                Settlements=S(new[]{"Ao","Mai","Kea","Tavi","Lua"}, new[]{"lo","na","ri","va"}, new[]{"a","oa","ua","i"}),
                Rivers=R(new[]{"Lua","Aoa","Mai"}, new[]{"na","loa","ri","ua"}) },
            new NamingStyle { Id="northern", DisplayName="Northern",
                FactionNames=new[]{"Hrafnfolk","Skeldar","Vargheim","Rimvald","Eldmark"},
                Settlements=S(new[]{"Hrafn","Skel","Varg","Rim","Eld"}, new[]{""}, new[]{"heim","mark","vik","dal","gard"}),
                Rivers=R(new[]{"Is","Frost","Varg"}, new[]{"elv","strom","vatn"}) },
            new NamingStyle { Id="ancient", DisplayName="Ancient",
                FactionNames=new[]{"Aurelion","Valerion","Castrava","Seraphex","Orthelion"},
                Settlements=S(new[]{"Aur","Val","Cas","Ser","Orth"}, new[]{"el","ar","eri"}, new[]{"ion","um","or","ex"}),
                Rivers=R(new[]{"Aur","Val","Ser"}, new[]{"is","ium","ara"}) },
            new NamingStyle { Id="jungle", DisplayName="Jungle",
                FactionNames=new[]{"Kumara","Itzali","Xomari","Yaneku","Ocela"},
                Settlements=S(new[]{"Ku","It","Xo","Ya","Oce"}, new[]{"ma","za","ri","na"}, new[]{"ra","li","ku","ta"}),
                Rivers=R(new[]{"Xi","Ku","Ama"}, new[]{"ri","na","lu"}) },
            new NamingStyle { Id="volcanic", DisplayName="Volcanic",
                FactionNames=new[]{"Magrath","Pyron","Cindor","Ashkar","Volruk"},
                Settlements=S(new[]{"Mag","Cin","Ash","Vol"}, new[]{"ra","do","ga"}, new[]{"th","or","ak","un"}),
                Rivers=R(new[]{"Lav","Cin","Ash"}, new[]{"or","ak","um"}) },
            new NamingStyle { Id="tundra", DisplayName="Tundra",
                FactionNames=new[]{"Aiskar","Norvik","Svalen","Istrad","Kelvor"},
                Settlements=S(new[]{"Nor","Sva","Kel","Ist"}, new[]{"va","ra"}, new[]{"ik","en","or","ad"}),
                Rivers=R(new[]{"Ice","Kel","Nor"}, new[]{"run","vik","el"}) },
            new NamingStyle { Id="marshfolk", DisplayName="Marshfolk",
                FactionNames=new[]{"Fenori","Bogren","Mirek","Lurash","Fenal"},
                Settlements=S(new[]{"Fen","Bog","Mir","Lu"}, new[]{"ra","ne"}, new[]{"i","ek","ash","al"}),
                Rivers=R(new[]{"Fen","Mir"}, new[]{"el","ash","or"}) },
            new NamingStyle { Id="scholar", DisplayName="Scholar",
                FactionNames=new[]{"Helion","Cyradis","Mentara","Othen","Lysara"},
                Settlements=S(new[]{"Hel","Cyr","Men","Lys"}, new[]{"io","ara"}, new[]{"on","is","um"}),
                Rivers=R(new[]{"Hel","Lys"}, new[]{"ion","ara"}) },
            new NamingStyle { Id="nomad", DisplayName="Nomad",
                FactionNames=new[]{"Rashun","Kelari","Torash","Mekhan","Yurak"},
                Settlements=S(new[]{"Ra","Ke","To","Me","Yu"}, new[]{"la","ra"}, new[]{"sh","ri","ak","an"}),
                Rivers=R(new[]{"Ra","Yu"}, new[]{"sh","ak","el"}) },
            new NamingStyle { Id="cliff", DisplayName="Cliff Dwellers",
                FactionNames=new[]{"Briskar","Claven","Torcliff","Rokhan","Skaran"},
                Settlements=S(new[]{"Bri","Cla","Rok","Ska"}, new[]{"va","ra"}, new[]{"en","an","or"}),
                Rivers=R(new[]{"Rok","Cla"}, new[]{"fall","run","or"}) },
            new NamingStyle { Id="delta", DisplayName="Delta",
                FactionNames=new[]{"Velora","Inari","Sorel","Amora","Pelan"},
                Settlements=S(new[]{"Ve","Ina","So","Ama"}, new[]{"lo","ra","ni"}, new[]{"ra","el","a"}),
                Rivers=R(new[]{"Ve","So"}, new[]{"len","ra","is"}) },
            new NamingStyle { Id="merchant", DisplayName="Merchant League",
                FactionNames=new[]{"Carovan","Mercalis","Tradin","Valcor","Serevon"},
                Settlements=S(new[]{"Car","Mer","Val","Ser"}, new[]{"o","a"}, new[]{"van","lis","cor"}),
                Rivers=R(new[]{"Mer","Val"}, new[]{"is","or","a"}) },
            new NamingStyle { Id="oracle", DisplayName="Oracle",
                FactionNames=new[]{"Omyra","Sephor","Ilun","Vaesh","Koriel"},
                Settlements=S(new[]{"Om","Sep","Ilu","Vae"}, new[]{"y","e","a"}, new[]{"ra","or","iel"}),
                Rivers=R(new[]{"Om","Ilu"}, new[]{"en","iel","or"}) },
        };

        public static readonly Dictionary<string, NamingStyle> ById = BuildById();
        private static Dictionary<string, NamingStyle> BuildById()
        {
            var d = new Dictionary<string, NamingStyle>();
            foreach (var s in Styles) d[s.Id] = s;
            return d;
        }

        /// <summary>Hybrid part 1: faction values -> cultural style.</summary>
        public static readonly Dictionary<string, string> StyleByValue = new Dictionary<string, string>
        {
            { "secrecy", "hidden_whisper" }, { "knowledge", "scholar" }, { "faith", "oracle" },
            { "order/hierarchy", "ancient" }, { "freedom", "nomad" }, { "tradition", "northern" }
        };

        /// <summary>Hybrid part 2: preferred biome -> biome style (keyed by (int)Biome).</summary>
        public static readonly Dictionary<int, string> StyleByBiome = new Dictionary<int, string>
        {
            { (int)Biome.Coast, "coastal_flowing" }, { (int)Biome.Riverlands, "delta" },
            { (int)Biome.Forest, "forest" }, { (int)Biome.DeepForest, "forest" },
            { (int)Biome.Jungle, "jungle" }, { (int)Biome.Desert, "desert" },
            { (int)Biome.Swamp, "swamp" }, { (int)Biome.Tundra, "tundra" },
            { (int)Biome.Highlands, "highland_hard" }, { (int)Biome.Mountains, "cliff" },
            { (int)Biome.Plains, "steppe" }, { (int)Biome.Volcanic, "volcanic" },
            { (int)Biome.SacredGroves, "forest" }
        };

        public static string Generate(SyllableGen g, Rng rng)
        {
            var pat = rng.Pick(g.Patterns);
            var sb = new StringBuilder();
            foreach (var slot in pat) sb.Append(rng.Pick(g.Parts[slot]));
            string s = sb.ToString();
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s;
        }
    }
}
