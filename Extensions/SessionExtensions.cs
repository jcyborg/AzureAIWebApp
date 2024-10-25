using Newtonsoft.Json;

namespace OpenAIChatBot.Extensions
{
    public static class SessionExtensions
    {
        // Set complex object in session
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        // Get complex object from session
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
