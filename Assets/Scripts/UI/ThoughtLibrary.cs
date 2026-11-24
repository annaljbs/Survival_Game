using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All the character's inner-voice lines (dark humour), sorted into pools.
/// Pick(pool) hands out the lines in random order and never repeats a line until the pool is used up.
/// Tone: the jokes are about bad luck, nature and technology, never about real victims.
/// </summary>
public static class ThoughtLibrary
{
    static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
    {
        // ---- doing things ----
        { "gather", new[] {
            "Gathering. The oldest job in history, and I'm still the worst at it.",
            "My ancestors did this for a million years. I'm already tired after ten minutes.",
            "Another armful. At this rate I'll be a lumberjack by Thursday. If there is a Thursday.",
            "Nature provides. Slowly, and with a lot of mosquitoes.",
            "Hunter-gatherer. Well, gatherer. Hunter is stretching it.",
            "If I survive, I'm never complaining about the supermarket again." } },
        { "search", new[] {
            "Rummaging through a stranger's luggage. Yes, I feel bad. No, I'm not stopping.",
            "Finders keepers, wreck edition.",
            "Somebody packed a lot of socks. None for me, of course.",
            "I've been to worse flea markets." } },
        { "water", new[] {
            "Water. The one thing that's always the answer, until it's the question.",
            "Hydration: a thing I used to take for granted, along with Wi-Fi.",
            "Cheers. To me, and to whatever lives in this stream." } },
        { "boil", new[] {
            "Boiled water. The difference between a drink and a 48-hour regret.",
            "Giardia can wait outside. It wasn't invited.",
            "It tastes like smoke and relief." } },
        { "fire_light", new[] {
            "Fire! Humans have done this for 400,000 years. I needed a lighter and a lot of luck.",
            "A fire. I'm basically a caveman with a better wardrobe.",
            "Warm, bright and probably visible from space. Hello, anyone?" } },
        { "fire_add", new[] {
            "One more log. The fire is the only one here that appreciates me.",
            "Feeding the fire. It's the only relationship I can currently maintain." } },
        { "frame", new[] {
            "Four sticks and a dream. Welcome to my architecture era.",
            "It leans a bit. So does the Tower of Pisa, and that's a tourist attraction." } },
        { "roof", new[] {
            "A roof. Real estate. I'm practically a landlord now.",
            "Rain, you can't have me. I have a roof and a very dry attitude." } },
        { "bed", new[] {
            "A bed of branches. Five stars, no reviews.",
            "The ground drains heat like a vampire drains... anything. A bed off the ground, finally." } },
        { "sleep_wake", new[] {
            "Slept. Woke up. Both outcomes better than expected.",
            "I dreamed of pizza. Now it's just branches and regret.",
            "Eight hours of sleep would've been nice. I got three and a half pinecones in the back." } },
        { "climb", new[] {
            "Climbing: fitness training for people who didn't sign up for fitness training.",
            "Up we go. If I fall, at least the view was nice for a second." } },
        { "signal", new[] {
            "Look at me. I'm a very visible person, in a very big forest.",
            "A signal. Please be seen by someone who is good at looking." } },
        { "refused", new[] {
            "Plan A failed. There was no plan B. There was barely a plan A.",
            "Turns out wanting something is not the same as having it.",
            "My to-do list and the universe have a disagreement.",
            "Not like that. Okay. Noted.",
            "Cannot do that right now. Story of my life." } },

        // ---- found things ----
        { "junk3", new[] {
            "Three pieces of junk. I'm starting a museum. The Museum of Bad Luck.",
            "Is this survival, or am I just a very slow garbage collector?" } },
        { "junk6", new[] {
            "Six useless things. Curator of the Wilderness Museum of Nothing.",
            "At this point my pockets are a landfill with a dream." } },

        // ---- missions ----
        { "m_salvage", new[] {
            "A bottle and a lighter. My entire wealth. Ranks me below most squirrels." } },
        { "m_water", new[] {
            "Water, fresh from the wilderness. Fresh from every animal's bathroom, too." } },
        { "m_materials", new[] {
            "Tinder, kindling, logs. Technically a bonfire kit. Emotionally a cry for help." } },
        { "m_fire", new[] {
            "Fire. Mission accomplished. Now I only have to not set myself on fire." } },
        { "m_boil", new[] {
            "Safe water. The most boring luxury in the world, and I'd kill for it. Figuratively. Mostly." } },
        { "m_shelter", new[] {
            "A shelter. Not pretty, not big, but it has walls. Okay, one wall. Roughly." } },
        { "m_sleep", new[] {
            "I slept in a forest. On purpose. Reality TV would pay for this." } },
        { "m_night", new[] {
            "Survived the night. The bar has never been lower, or more satisfying." } },

        // ---- the phone ----
        { "phone_more", new[] {
            "Checking again won't change anything. I know. I'm doing it anyway.",
            "Still no signal. At this point it's an expensive mirror.",
            "Airplane mode is off. The whole plane is off. Everything is off." } },

        // ---- places ----
        { "site_STREAM MEADOW", new[] {
            "Lovely meadow. Right next to the water. What could possibly flood?" } },
        { "site_FOREST EDGE", new[] {
            "Forest edge. Trees for the wind, bugs for company." } },
        { "site_ROCK LEDGE", new[] {
            "A ledge with a view. Perfect for photos, if my phone had a pulse." } },
        { "site_CRASH SITE", new[] {
            "The crash site. Home sweet wreck." } },
        { "site_NORTH CLEARING", new[] {
            "An empty clearing. Peaceful. Suspiciously peaceful." } },
        { "river", new[] {
            "Cold. So cold. My toes have filed for divorce.",
            "Wading through ice water: the free spa treatment nobody asked for." } },
        { "thick", new[] {
            "A shortcut through the thick forest. Shortcuts: the leading cause of long stories.",
            "Every branch has opinions about my face." } },

        // ---- weather and time ----
        { "rain_no_roof", new[] {
            "Rain on the head, cold in the bones. Mother Nature's cheapest shower.",
            "I'm getting wet. Wet clothes lose most of their insulation. Fun fact, miserable fact.",
            "No roof, lots of sky. The sky is very generous with water today." } },
        { "cosy", new[] {
            "Dry. Warm-ish. Smug. The rain can try its best.",
            "The roof holds. Rain drums on it like a very bad band, and I couldn't be happier.",
            "Inside, dry, and not dead. A strong day by current standards." } },
        { "flood", new[] {
            "Water is creeping in under my shelter. I chose low ground. Of course I did.",
            "A roof doesn't help when the floor is a puddle. Lesson learned, sogginess pending." } },
        { "wet", new[] {
            "My clothes are soaked. Cotton is a cold, wet hug.",
            "Soaked through. At least I'm not thirsty on the outside." } },
        { "sunset", new[] {
            "Sunset. Beautiful. Also: the deadline for my bad decisions.",
            "The sun is leaving. Everyone leaves." } },
        { "midnight", new[] {
            "Midnight. The hour of wolves, bad ideas and cold feet.",
            "It's midnight. Nobody knows I'm here. Okay, that's the real horror movie." } },
        { "witching", new[] {
            "3 a.m. The hour when the body is at its coldest and the mind at its funniest.",
            "I've been awake since the crash. My eyes feel like sandpaper in a fan." } },
        { "sunrise", new[] {
            "Sunrise. A new day of being not rescued.",
            "Morning. The birds are singing. Show-offs." } },
        { "noon2", new[] {
            "Day two, noon. Nothing like a rescue to ruin a good survival story.",
            "Still alive. I'd like that on my CV." } },
        { "sunset2", new[] {
            "The second sunset. If anybody's counting: that's two sunsets too many.",
            "Another night. I'm getting better at this. That worries me." } },
        { "fire_dying", new[] {
            "The fire is dying. Like my social life, but louder.",
            "Last embers. Time to find out who I really am: a person with logs, or a person without." } },

        // ---- moving and waiting ----
        { "idle", new[] {
            "Staring into the forest. The forest stares back. Nobody blinks.",
            "I'm not doing nothing. I'm conserving energy. Ask the squirrels.",
            "A moment of silence for my phone battery.",
            "Standing still is also a decision. A bad one, but a decision.",
            "I could really use a plan right now. Any plan." } },
        { "walk", new[] {
            "Walking. The most overrated survival skill, after hope.",
            "My legs are asking how far it is. My map is asking why I'm asking.",
            "Every tree looks the same. Trees: 1, Navigation: 0.",
            "If I'm walking in circles, at least I'm consistent.",
            "Somewhere out there is a road. Probably." } },

        // ---- bars ----
        { "low_Energy", new[] {
            "I'm tired. Not 'long day' tired. 'Existentially' tired.",
            "My legs have started sending me strongly worded emails." } },
        { "low_Water", new[] {
            "Thirsty. Funny how a whole forest can feel like a desert.",
            "My mouth is dry. My sense of humour, as well." } },
        { "low_Food", new[] {
            "My stomach is growling. At least somebody's still talking to me.",
            "Hungry. I'd eat anything, but only one of those anything is a good idea." } },
        { "low_Warmth", new[] {
            "It's cold. The kind of cold that makes you reconsider everything, including this career.",
            "My fingers are turning interesting colours." } },
        { "crit_Energy", new[] {
            "My legs have filed a complaint. It was rejected, like everything else lately.",
            "If I lie down now, will anybody notice? Rhetorical. Please let somebody notice.",
            "I'm so tired the trees are starting to look comfy." } },
        { "crit_Water", new[] {
            "Tongue like sandpaper. A whole stream out there, and it feels like a rumour.",
            "My thoughts are drying up. The good ones first.",
            "Last time I was this thirsty was the festival. This is worse." } },
        { "crit_Food", new[] {
            "My stomach is composing a farewell speech.",
            "I'm so hungry I'm considering the pinecone. Reconsidering. Not yet.",
            "Hunger: the cheapest motivation tool there is." } },
        { "crit_Warmth", new[] {
            "I can't feel my fingers. At least I can't feel the rest either.",
            "Shivering. The body's last attempt at a dance party.",
            "I'm so cold that my teeth have formed a band." } },
    };

    // Phone: the lines come in order, and the last pool lines repeat.
    static readonly string[] PhoneSequence =
    {
        "No bars. I stared at it like it owed me money.",
        "Last cached screen: \"How to make sourdough starter\". Priorities, past me.",
        "An unsent text: \"brb, flying over the woods, signal is trash lol\". Sent: never. Irony: always.",
        "Battery: 2%. Enough for exactly one more disappointment.",
    };

    static readonly Dictionary<string, Queue<string>> bags = new Dictionary<string, Queue<string>>();

    /// <summary>A random line from the pool, never the same line twice before the pool is used up. Null if the pool is unknown.</summary>
    public static string Pick(string pool)
    {
        if (!Pools.TryGetValue(pool, out var lines) || lines.Length == 0) return null;
        if (!bags.TryGetValue(pool, out var bag) || bag.Count == 0)
        {
            var shuffled = new List<string>(lines);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            bag = new Queue<string>(shuffled);
            bags[pool] = bag;
        }
        return bag.Dequeue();
    }

    public static bool Has(string pool) => Pools.ContainsKey(pool);

    /// <summary>The n-th phone check (1 = first). The first lines are fixed in order, after that random ones.</summary>
    public static string Phone(int n)
    {
        return n <= PhoneSequence.Length ? PhoneSequence[n - 1] : Pick("phone_more");
    }

    /// <summary>Forget what was handed out (new run).</summary>
    public static void Reset() => bags.Clear();
}
