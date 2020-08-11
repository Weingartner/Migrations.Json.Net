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
 
 You do need to specify to Json.NET that you want some custom behavior to happen while deserializing:
 
	string myData = ...; // Read from file?
 	var serializerSettings = new JsonSerializerSettings();
	serializerSettings.Converters.Add(new MigrationConverter(new HashBasedDataMigrator<JToken>(new JsonVersionUpdater())));
	MyData myDataDeserialized = JsonConvert.DeserializeObject<MyData>(myData, serializerSettings);
 
 But that's all! If you want, you can stop reading.
 
 Understanding the basics
================================

 Let's explain a bit what happens here.
 
      [Migratable("")]

This attribute marks the MyData class wrt. the migration system, so that it knows it needs to search for a migration method. The migration system does not parse classes that do not have this attribute.

      private static JObject Migrate_1(JObject data, JsonSerializer serializer)

The signature of the migration method should follow a specific format. First, it should be a static method on the class. It should also have the types defined above for its return type and arguments. Finally, its name should follow the following pattern: Migrate_number, where number is replaced by the version number of this class. A version number of 0 means that it's the original version of this class, and that there is no migration method necessary (hence the name "Migrate_0" is not possible). A version number greater than 0, e.g. n, means that the method is executed to migrate serialized data from version n - 1 to version n.

	data["name"] = data["firstName"] + " " + data["lastName"];

Inside the method, you can modify the JObject that represents the deserialized data that comes from version n - 1 so that it can automatically be read by version n. In the example above, data would contain something that corresponds to this:

	{
		"firstName": "John",
		"lastName": "Doe"
	}

And this in the new format, what you want is this:

	{
		"name": "John Doe"
	}

That's why you need to add a "name" field to the data object, setting its value to the concatenated values of firstName and lastName.

NB: If you paid attention, you may have realized that the code above actually produces this:

	{
		"name": "John Doe",
		"firstName": "John",
		"lastName": "Doe"
	}

Indeed, we are not removing the existing "firstName" and "lastName" fields from the data object. But it doesn't matter! Because by default, Json.NET simply ignores json fields that do not appear in the deserialized class. If you are deserializing with a setting that makes Json.NET treat this as an error, you will want to remove those from the data object in the migration method.

 Versioning
================================

Each class has its own version field that is used by the migration library to know when to invoke which migration method. If a class doesn't define such a version field, the migration system will consider the class to be of version 0 - so it will invoke all the migration methods in order.

It means that in most cases, you will want to add this version field to your serialized classes. It takes the following form:

	class MyData
	{
		int Version = 1;
	}

So an integer-typed member variable whose name is "Version". 

The migration system will automatically increment the version field when deserializing. Let's take our initial example, but now adding the Version field:

      [Migratable("")]
      class MyData
      {
		int Version = 1;
		string name = "John Doe";
            
		private static JObject Migrate_1(JObject data, JsonSerializer serializer)
		{
			data["name"] = data["firstName"] + " " + data["lastName"];
			return data;
		}
      }

If this class is used to deserialize the following data:

	{
		"firstName": "John",
		"lastName": "Doe"
	}

then the result would be this:

	{
		"Version": 1
		"name": "John Doe",
		"firstName": "John",
		"lastName": "Doe"
	}

But it would be the same result if the loaded data had a Version field of value 0, e.g.:

	{
		"Version": 0,
		"firstName": "John",
		"lastName": "Doe"
	}

Because the migration method Migrate_1 would have parsed the Version field and incremented it to 1.

It follows that the Version field should be incremented each time your class is modified with changes that are backwards incompatible (otherwise you need to know what you're doing). Basically whenever you have the need to add a migration method, also remember to increment the Version field to the highest suffix of the migration methods. For instance, if you have the following migration methods: Migrate_1, Migrate_2, Migrate_3, then the Version field should have 3 as its default value, so that new instances of that class have the correct version number and do not invoke incorrect migration methods upon deserialization.

 Advanced usage
================================

This library supports nested classes as well, out of the box. Simply remember to mark each class as Migratable and to add a Version field to each class. Example:

      [Migratable("")]
      class Person
      {
		int Version = 1;
		string name = "John Doe";
		Address address = new Address();
            
		private static JObject Migrate_1(JObject data, JsonSerializer serializer)
		{
			data["name"] = data["firstName"] + " " + data["lastName"];
			return data;
		}
      }

      [Migratable("")]
      class Address
      {
		int Version = 1;
		string streetName = "Champs-Élysées";
            
		private static JObject Migrate_1(JObject data, JsonSerializer serializer)
		{
			data["streetName"] = data["street"]; // the field 'street' was renamed to 'streetName'
			return data;
		}
      }

The automatic tests in this repository demonstrate various use cases, it can be an additional source of inspiration if this readme is not explicit enough. 
