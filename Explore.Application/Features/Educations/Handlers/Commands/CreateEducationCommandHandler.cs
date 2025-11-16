using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Educations.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Educations.Handlers.Commands
{
    public class CreateEducationCommandHandler : IRequestHandler<CreateEducationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IMapper _mapper;

        public CreateEducationCommandHandler(IEducationRepository educationRepository, IMapper mapper)
        {
            _educationRepository = educationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEducationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            //var validator = new CreateEducationDtoValidator();
            //var validationResult = await validator.ValidateAsync(request.EducationDto);
            //if (!validationResult.IsValid)
            //{
            //    response.Success = false;
            //    response.Message = "Education creation failed.";
            //    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            //    return response;
            //}

            var education = _mapper.Map<Domain.Education>(request.EducationDto);
            education = await _educationRepository.Create(education);

            response.Success = true;
            response.Id = education.Id;
            response.Message = "Education program created successfully.";

            return response;
        }
    }
}using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Educations.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Educations.Handlers.Commands
{
    public class CreateEducationCommandHandler : IRequestHandler<CreateEducationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IMapper _mapper;

        public CreateEducationCommandHandler(IEducationRepository educationRepository, IMapper mapper)
        {
            _educationRepository = educationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEducationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            //var validator = new CreateEducationDtoValidator();
            //var validationResult = await validator.ValidateAsync(request.EducationDto);
            //if (!validationResult.IsValid)
            //{
            //    response.Success = false;
            //    response.Message = "Education creation failed.";
            //    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            //    return response;
            //}

            var education = _mapper.Map<Domain.Education>(request.EducationDto);
            education = await _educationRepository.Create(education);

            response.Success = true;
            response.Id = education.Id;
            response.Message = "Education program created successfully.";

            return response;
        }
    }
}
