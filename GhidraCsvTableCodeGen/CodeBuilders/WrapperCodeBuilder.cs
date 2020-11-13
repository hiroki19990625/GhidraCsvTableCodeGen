using GhidraCsvTableCodeGen.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhidraCsvTableCodeGen.CodeBuilders
{
    class WrapperCodeBuilder
    {
        private readonly WrapperCommandOptions options;

        public WrapperCodeBuilder(WrapperCommandOptions options)
        {
            this.options = options;
        }

        public void Build()
        {
            throw new NotImplementedException();
        }
    }
}
