using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UsersDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TypeUser Type { get; set; }
        public int Status { get; set; }
        public EmpresasDTO Empresas { get; set; } = new EmpresasDTO();
    }

    public class UsersPagedDTO
    {
        public List<UsersDTO> Result { get; set; } = new List<UsersDTO>();
        public int PageCount { get; set; }
    }
}
