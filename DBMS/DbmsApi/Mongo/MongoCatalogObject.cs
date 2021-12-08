﻿using DbmsApi.API;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace DbmsApi.Mongo
{
    public class MongoCatalogObject : MongoDocument
    {
        public string Name;
        public string TypeId;

        public Properties Properties;
        public List<KeyValuePair<string, string>> Tags = new List<KeyValuePair<string, string>>();
        public List<MeshRep> MeshReps = new List<MeshRep>();
    }

    public class CatalogObjectReference : BaseObject
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string CatalogId;
    }
}