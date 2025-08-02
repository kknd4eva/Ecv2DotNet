using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecv2DotNet
{
    public interface IValidator
    {
        Task<bool> IsValidEcv2SignatureAsync();
    }
}
