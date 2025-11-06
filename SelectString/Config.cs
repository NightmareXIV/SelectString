using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelectString;
[Serializable]
public class Config : IEzConfig
{
    public List<string> DisabledAddons = [];
    public bool NoRaise = true;
}
