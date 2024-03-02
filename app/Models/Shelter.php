<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsToMany;
use Illuminate\Database\Eloquent\Relations\MorphOne;

class Shelter extends Model
{
    use HasFactory;

    protected $fillable = [
        'name',
        'description',
        'capacity',
    ];

    public function address(): MorphOne
    {
        return $this->morphOne(Address::class, 'morphable');
    }

    public function users(): BelongsToMany
    {
        return $this->belongsToMany(User::class)->using(ShelterUser::class)->withPivot('role');
    }
}
