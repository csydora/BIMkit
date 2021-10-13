﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbmsApi.API
{
    public class CatalogObject
    {
        public string CatalogID;
        public string Name;
        public string TypeId;
        public Properties Properties;
        public List<Component> Components = new List<Component>();
    }
}