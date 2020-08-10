Migrations.Json.Net
===================

A simple framework for data migrations using Newtonsoft Json.Net. o

Quickstart
================================

Suppose you have the following class that gets serialized in your project:

      class MyData
      {
            string firstName = "John";
            string lastName = "Doe";
      }

But in a later revision, you change it to this:

      class MyData
      {
            string name = "John Doe";
      }

Oh noes! It's not backwards compatible. Well, with this library, you simply have to apply the following modifications:

      [Migratable("")]
      class MyData
      {
            string name = "John Doe";
            
            private static JObject Migrate_1(JObject data, JsonSerializer serializer)
		{
			data["name"] = data["firstName"] + " " + data["lastName"];
			return data;
		}
      }
 
 And poof! When deserializing a json file with the previous format, it will automatically concatenate the previous firstName and lastName fields into the new 'name' field.
 
 Let's explain a bit what happens here.
 
      [Migratable("")]

This attribute marks the MyData class wrt. the migration system, so that it knows it needs to search for a migration method inside. The migration system does not parse classes that does not have this attribute.

      private static JObject Migrate_1(JObject data, JsonSerializer serializer)

The signature of the migration method should follow a specific format. First, it should be a static method on the class. It should also have the types defined above for its return type and arguments. Finally, its name should follow the following pattern: Migrate_number, where number is replaced by the version number of this class. A version number of 0 means that it's the original version of this class, and that there is no migration method necessary (hence the name "Migrate_0" is not possible). A version number greater than 0, e.g. n, means that the method is executed to migrate serialized data from version n - 1 to version n.

