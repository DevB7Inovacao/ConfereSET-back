
using AutoMapper;
using Core.DTO;
using Core.Models;

namespace Core.Mapping
{
	public class EmpresasMappingProfile : Profile
	{
		public EmpresasMappingProfile()
		{
			CreateMap<EmpresasDTO, Empresas>();

			CreateMap<Empresas, EmpresasDTO>();
		}
	}
}