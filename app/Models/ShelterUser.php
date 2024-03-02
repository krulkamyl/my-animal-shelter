<?php

namespace App\Models;

use App\Enums\ShelterUserRole;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class ShelterUser extends Model
{
    use HasFactory;

    protected $fillable = [
        'role',
    ];

    protected $casts = [
        'role' => ShelterUserRole::class,
    ];
}
