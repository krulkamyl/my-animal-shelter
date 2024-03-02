<?php

namespace App\Enums;

enum ShelterUserRole: string
{
    case OWNER = 'owner';
    case EMPLOYEE = 'employee';
    case VOLUNTEER = 'volunteer';
}
