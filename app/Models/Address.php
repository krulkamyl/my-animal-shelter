<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\MorphTo;

class Address extends Model
{
    use HasFactory;

    protected $fillable = [
        'first_name',
        'last_name',
        'company_name',
        'address',
        'city',
        'state',
        'zip',
        'phone',
        'email',
        'website',
    ];

    public function morphable(): MorphTo
    {
        return $this->morphTo();
    }
}
